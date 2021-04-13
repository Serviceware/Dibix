using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Dapper;

namespace Dibix.Testing
{
    public abstract class DatabaseTestBase<TConfiguration> : TestBase<TConfiguration>, IDisposable where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Fields
        private Action _removeTraceListener;
        #endregion

        #region Protected Methods
        protected override void OnTestContextInitialized()
        {
            TraceSource dibixTraceSource = GetDibixTraceSource();
            TraceListener traceListener = base.TestOutputHelper.CreateTraceListener();
            dibixTraceSource.Listeners.Add(traceListener);
            this._removeTraceListener = () => dibixTraceSource.Listeners.Remove(traceListener);
        }

        protected static async Task<TResult> ExecuteDatabaseAction<TResult>(TConfiguration configuration, Func<IDatabaseAccessor, Task<TResult>> action)
        {
            IDatabaseAccessorFactory databaseAccessorFactory = CreateDatabaseAccessorFactory(configuration);
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return await action(accessor).ConfigureAwait(false);
            }
        }

        protected static async Task ExecuteStoredProcedure(TConfiguration configuration, string storedProcedureName, Action<IParameterBuilder> parameters = null, int commandTimeout = 600)
        {
            IDatabaseAccessorFactory databaseAccessorFactory = CreateDatabaseAccessorFactory(configuration);
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                   IParameterBuilder parameterBuilder = accessor.Parameters();
                parameters?.Invoke(parameterBuilder);
                await accessor.ExecuteAsync(storedProcedureName, CommandType.StoredProcedure, commandTimeout, parameterBuilder.Build(), CancellationToken.None).ConfigureAwait(false);
            }
        }

        protected static IDatabaseAccessorFactory CreateDatabaseAccessorFactory(TConfiguration configuration) => new DapperDatabaseAccessorFactory(configuration.Database.ConnectionString);
        #endregion

        #region Private Methods
        private static TraceSource GetDibixTraceSource()
        {
            const string fieldName = "TraceSource";
            
            FieldInfo field = typeof(DatabaseAccessor).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new InvalidOperationException($"Field '{fieldName}' is not defined for type '{typeof(TraceSource)}'");

            TraceSource traceSource = (TraceSource)field.GetValue(null);
            if (traceSource == null)
                throw new InvalidOperationException("Could not resolve trace souce: Dibix.Sql");

            traceSource.Switch.Level = SourceLevels.Information;
            return traceSource;
        }

        private static TraceSource GetDibixTraceSource_()
        {
            FieldInfo traceSourcesField = typeof(TraceSource).GetField("s_tracesources", BindingFlags.NonPublic | BindingFlags.Static);
            ICollection<WeakReference> traceSources = (ICollection<WeakReference>)traceSourcesField.GetValue(null);
            TraceSource traceSource = traceSources.Select(x => x.Target)
                                                  .Cast<TraceSource>()
                                                  .Single(x => x.Name == "Dibix.Sql");
            return traceSource;
        }
        #endregion

        #region Nested Types
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
        #endregion

        #region IDisposable Members
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this._removeTraceListener?.Invoke();
            }
        }
        #endregion
    }
}