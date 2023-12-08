using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dibix.Testing.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing.Data
{
    public static class DatabaseTestUtility
    {
        public static IDatabaseAccessorFactory CreateDatabaseAccessorFactory<TConfiguration>(TestContext testContext, TestConfigurationValidationBehavior configurationValidationBehavior = TestDefaults.ValidationBehavior) where TConfiguration : DatabaseConfigurationBase, new()
        {
            TConfiguration configuration = TestConfigurationLoader.Load<TConfiguration>(testContext, configurationValidationBehavior);
            return CreateDatabaseAccessorFactory(configuration);
        }
        public static IDatabaseAccessorFactory CreateDatabaseAccessorFactory<TConfiguration>(TConfiguration configuration, int? defaultCommandTimeout = null) where TConfiguration : DatabaseConfigurationBase, new()
        {
            return new DapperDatabaseAccessorFactory(configuration.Database.ConnectionString, RaiseErrorWithNoWaitBehavior.ExecuteScalar, defaultCommandTimeout);
        }

        // RAISERROR WITH NOWAIT is useful to receive immediate progress of a long running command.
        // To make sure the messages are immediately received, there are two options:
        // 1. SqlConnection.FireInfoMessageEventOnUserErrors
        // => This approach is generic but comes with the big downside, that all errors are sent to the InfoMessage event handler and no exception is thrown.
        //    They could be rethrown within the event handler, but due to the fact, that only severe exceptions are going through, and the call stack would be obscured by the event handler, makes this a very dirty option.
        // 2. Don't use SqlCommand.ExecuteNonQuery
        // => This approach is not generic and requires the caller to actually never use ExecuteNonQuery, even if it makes sense.
        //    It's still the best option, because it doesn't require custom exception handling like with option 1.
        //    To ensure the caller doesn't use ExecuteNonQuery, we can provide a custom implementation that always delegates ExecuteNonQuery to ExecuteScalar.
        private enum RaiseErrorWithNoWaitBehavior
        {
            None,
            FireInfoMessageEventOnUserErrors,
            ExecuteScalar
        }

        private sealed class DapperDatabaseAccessorFactory : IDatabaseAccessorFactory
        {
            private readonly string _connectionString;
            private readonly RaiseErrorWithNoWaitBehavior _raiseErrorWithNoWaitBehavior;
            private readonly int? _defaultCommandTimeout;

            public DapperDatabaseAccessorFactory(string connectionString, RaiseErrorWithNoWaitBehavior raiseErrorWithNoWaitBehavior, int? defaultCommandTimeout)
            {
                _connectionString = connectionString;
                _raiseErrorWithNoWaitBehavior = raiseErrorWithNoWaitBehavior;
                _defaultCommandTimeout = defaultCommandTimeout;
            }

            public IDatabaseAccessor Create()
            {
                SqlConnection connection = new SqlConnection(_connectionString);

                if (_raiseErrorWithNoWaitBehavior == RaiseErrorWithNoWaitBehavior.FireInfoMessageEventOnUserErrors)
                {
                    connection.FireInfoMessageEventOnUserErrors = true;
                    connection.InfoMessage += OnInfoMessage;
                }

                return new DapperDatabaseAccessor(connection, _raiseErrorWithNoWaitBehavior, _defaultCommandTimeout);
            }

            // When FireInfoMessageEventOnUserErrors is true, errors will trigger an info message event aswell, without throwing an exception.
            // To restore the original behavior for errors, we have to throw ourselves.
            private static void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
            {
                bool isError = e.Errors.Cast<SqlError>().Aggregate(false, (current, sqlError) => current || sqlError.Class > 10);
                if (!isError)
                    return;

                SqlErrorCollection errorCollection = e.Errors;
                string serverVersion = null;
                MethodInfo createExceptionMethod = typeof(SqlException).SafeGetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(SqlErrorCollection), typeof(string) });
                SqlException exception = (SqlException)createExceptionMethod.Invoke(null, new object[] { errorCollection, serverVersion });

                // Exceptions within the InfoMessage handler will generally be caught and traced.
                // Unless they are of a severe exception type.
                throw new AccessViolationException(exception.Message, exception);
            }
        }

        private sealed class DapperDatabaseAccessor : Dapper.DapperDatabaseAccessor
        {
            private readonly RaiseErrorWithNoWaitBehavior _raiseErrorWithNoWaitBehavior;
            private readonly int? _defaultCommandTimeout;

            public DapperDatabaseAccessor(DbConnection connection, RaiseErrorWithNoWaitBehavior raiseErrorWithNoWaitBehavior, int? defaultCommandTimeout) : base(connection, defaultCommandTimeout: defaultCommandTimeout)
            {
                _raiseErrorWithNoWaitBehavior = raiseErrorWithNoWaitBehavior;
                _defaultCommandTimeout = defaultCommandTimeout;
            }

            // ExecuteNonQuery is optimized, and will not process any messages, so RAISERROR WITH NOWAIT will not work.
            // Here we override the underlying behavior, without the caller having to do it.
            protected override int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout)
            {
                if (_raiseErrorWithNoWaitBehavior != RaiseErrorWithNoWaitBehavior.ExecuteScalar)
                    return base.Execute(commandText, commandType, parameters, commandTimeout);

                _ = base.Connection.ExecuteScalar(commandText, CollectParameters(parameters), transaction: null, commandTimeout ?? _defaultCommandTimeout, commandType);
                return default;
            }
            protected override async Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken)
            {
                if (_raiseErrorWithNoWaitBehavior != RaiseErrorWithNoWaitBehavior.ExecuteScalar)
                    return await base.ExecuteAsync(commandText, commandType, parameters, commandTimeout, cancellationToken).ConfigureAwait(false);

                CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), transaction: null, commandTimeout ?? _defaultCommandTimeout, commandType, cancellationToken: cancellationToken);
                _ = await base.Connection.ExecuteScalarAsync(command).ConfigureAwait(false);
                return default;
            }
        }
    }
}