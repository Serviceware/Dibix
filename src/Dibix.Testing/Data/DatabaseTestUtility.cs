using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Dibix.Dapper;
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
            return new DapperDatabaseAccessorFactory(configuration.Database.ConnectionString);
        }

        private sealed class DapperDatabaseAccessorFactory : IDatabaseAccessorFactory
        {
            private readonly string _connectionString;

            public DapperDatabaseAccessorFactory(string connectionString) => this._connectionString = connectionString;

            public IDatabaseAccessor Create()
            {
                SqlConnection connection = new SqlConnection(this._connectionString)
                {
                    FireInfoMessageEventOnUserErrors = true // Important for tracing when using RAISERROR WITH NOWAIT
                };
                connection.InfoMessage += OnInfoMessage;
                return new DapperDatabaseAccessor(connection);
            }

            // We are using RAISERROR WITH NOWAIT to track long running progress.
            // This however requires the property FireInfoMessageEventOnUserErrors to be set to true.
            // When this is true, errors will trigger an info message event aswell, without throwing an exception.
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
    }
}