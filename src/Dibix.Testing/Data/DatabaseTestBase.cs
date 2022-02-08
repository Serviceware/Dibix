using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Testing.Data
{
    public abstract class DatabaseTestBase<TConfiguration> : TestBase<TConfiguration>, IDisposable where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Fields
        private Action _removeTraceListener;
        #endregion

        #region Protected Methods
        protected override Task OnTestInitialized()
        {
            TraceSource dibixTraceSource = GetDibixTraceSource();
            TraceListener traceListener = base.TestOutputHelper.CreateTraceListener();
            dibixTraceSource.Listeners.Add(traceListener);
            this._removeTraceListener = () => dibixTraceSource.Listeners.Remove(traceListener);
            return Task.CompletedTask;
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

        protected static IDatabaseAccessorFactory CreateDatabaseAccessorFactory(TConfiguration configuration) => DatabaseTestUtility.CreateDatabaseAccessorFactory(configuration);
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