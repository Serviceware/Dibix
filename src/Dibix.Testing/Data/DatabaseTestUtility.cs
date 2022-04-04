﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing.Data
{
    public static class DatabaseTestUtility
    {
        public static IDatabaseAccessorFactory CreateDatabaseAccessorFactory<TConfiguration>(TestContext testContext) where TConfiguration : DatabaseConfigurationBase, new()
        {
            TConfiguration configuration = TestConfigurationLoader.Load<TConfiguration>(testContext);
            return CreateDatabaseAccessorFactory(configuration);
        }
        public static IDatabaseAccessorFactory CreateDatabaseAccessorFactory<TConfiguration>(TConfiguration configuration) where TConfiguration : DatabaseConfigurationBase, new()
        {
            return new DapperDatabaseAccessorFactory(configuration.Database.ConnectionString, RaiseErrorWithNoWaitBehavior.ExecuteScalar);
        }

        // RAISERROR WITH NOWAIT is useful to receive immediate progress of a long running command.
        // To make sure the messages are immediately received, there are two options:
        // 1. SqlConnection.FireInfoMessageEventOnUserErrors
        // => This approach is generic but comes with the big downside, that all errors are sent to the InfoMessage and no exception is thrown.
        //    They can be rethrown in a way, but the fact, that only severe exceptions are going through, and the call stack is confusing, makes this a very dirty option.
        // 2. Don't use SqlCommand.ExecuteNonQuery
        // => This approach is not generic and requires the caller to actually never use ExecuteNonQuery, even if it makes sense.
        //    It's still the best option, because it doesn't require custom exception handling like with option 1.
        //    To ensure the caller doesn't use ExecuteNonQuery, we can provide a custom implementation that always replaces ExecuteNonQuery with ExecuteScalar.
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

            public DapperDatabaseAccessorFactory(string connectionString, RaiseErrorWithNoWaitBehavior raiseErrorWithNoWaitBehavior)
            {
                this._connectionString = connectionString;
                this._raiseErrorWithNoWaitBehavior = raiseErrorWithNoWaitBehavior;
            }

            public IDatabaseAccessor Create()
            {
                SqlConnection connection = new SqlConnection(this._connectionString);

                if (this._raiseErrorWithNoWaitBehavior == RaiseErrorWithNoWaitBehavior.FireInfoMessageEventOnUserErrors)
                {
                    connection.FireInfoMessageEventOnUserErrors = true;
                    connection.InfoMessage += OnInfoMessage;
                }

                return new DapperDatabaseAccessor(connection, this._raiseErrorWithNoWaitBehavior);
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
                MethodInfo createExceptionMethod = typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(string) }, null);
                SqlException exception = (SqlException)createExceptionMethod.Invoke(null, new object[] { errorCollection, serverVersion });

                // Exceptions within the InfoMessage handler will generally be caught and traced.
                // Unless they are of a severe exception type.
                throw new AccessViolationException(exception.Message, exception);
            }
        }

        private sealed class DapperDatabaseAccessor : Dapper.DapperDatabaseAccessor
        {
            private readonly RaiseErrorWithNoWaitBehavior _raiseErrorWithNoWaitBehavior;

            public DapperDatabaseAccessor(DbConnection connection, RaiseErrorWithNoWaitBehavior raiseErrorWithNoWaitBehavior) : base(connection)
            {
                this._raiseErrorWithNoWaitBehavior = raiseErrorWithNoWaitBehavior;
            }

            // ExecuteNonQuery is optimized, and will not process any messages, so RAISERROR WITH NOWAIT will not work
            // Here we override the underlying behavior, without the caller having to do it.
            protected override int Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters)
            {
                if (this._raiseErrorWithNoWaitBehavior != RaiseErrorWithNoWaitBehavior.ExecuteScalar)
                    return base.Execute(commandText, commandType, commandTimeout, parameters);

                _ = base.Connection.ExecuteScalar(commandText, CollectParameters(parameters), transaction: null, commandTimeout, commandType);
                return default;
            }
            protected override async Task<int> ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken)
            {
                if (this._raiseErrorWithNoWaitBehavior != RaiseErrorWithNoWaitBehavior.ExecuteScalar)
                    return await base.ExecuteAsync(commandText, commandType, commandTimeout, parameters, cancellationToken);

                CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), transaction: null, commandTimeout, commandType, cancellationToken: cancellationToken);
                _ = await base.Connection.ExecuteScalarAsync(command).ConfigureAwait(false);
                return default;
            }
        }
    }
}