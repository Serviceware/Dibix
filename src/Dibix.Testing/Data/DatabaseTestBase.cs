﻿using System;
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
        private readonly Lazy<IDatabaseAccessorFactory> _databaseAccessorFactoryAccessor;
        private Action _removeTraceListener;
        #endregion

        #region Properties
        protected IDatabaseAccessorFactory DatabaseAccessorFactory => this._databaseAccessorFactoryAccessor.Value;
        #endregion

        #region Constructor
        protected DatabaseTestBase()
        {
            this._databaseAccessorFactoryAccessor = new Lazy<IDatabaseAccessorFactory>(() => CreateDatabaseAccessorFactory());
        }
        #endregion

        #region Protected Methods
        protected override async Task OnTestInitialized()
        {
            await base.OnTestInitialized().ConfigureAwait(false);

            TraceSource dibixTraceSource = GetDibixTraceSource();
            TraceListener traceListener = base.TestOutputHelper.CreateTraceListener();
            dibixTraceSource.Listeners.Add(traceListener);
            this._removeTraceListener = () => dibixTraceSource.Listeners.Remove(traceListener);
        }

        protected async Task<TResult> ExecuteDatabaseAction<TResult>(Func<IDatabaseAccessor, Task<TResult>> action)
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                return await action(accessor).ConfigureAwait(false);
            }
        }

        protected async Task ExecuteStoredProcedure(string storedProcedureName, Action<IParameterBuilder> parameters = null, int commandTimeout = 30)
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                IParameterBuilder parameterBuilder = accessor.Parameters();
                parameters?.Invoke(parameterBuilder);
                await accessor.ExecuteAsync(storedProcedureName, CommandType.StoredProcedure, parameterBuilder.Build(), commandTimeout, CancellationToken.None).ConfigureAwait(false);
            }
        }

        protected IDatabaseAccessorFactory CreateDatabaseAccessorFactory(int? commandTimeout = 30) => DatabaseTestUtility.CreateDatabaseAccessorFactory(base.Configuration, commandTimeout);
        #endregion

        #region Private Methods
        private static TraceSource GetDibixTraceSource()
        {
            const string fieldName = "TraceSource";

            Type type = typeof(DatabaseAccessor);
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new InvalidOperationException($"Could not find 'private static {fieldName}' field on type '{type}'");

            TraceSource traceSource = (TraceSource)field.GetValue(null);
            if (traceSource == null)
                throw new InvalidOperationException($"'private static {fieldName}' field on type '{type}' is null");

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