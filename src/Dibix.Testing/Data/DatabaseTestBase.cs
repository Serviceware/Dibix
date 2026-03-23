using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Dibix.Testing.Data
{
    public abstract class DatabaseTestBase<TConfiguration> : TestBase<TConfiguration>, IDisposable where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Fields
        private static readonly DibixTraceSource[] DibixTraceSources = [DibixTraceSource.Sql, DibixTraceSource.Accessor];
        private readonly Lazy<IDisposableDatabaseAccessorFactory> _databaseAccessorFactoryAccessor;
        #endregion

        #region Properties
        protected IDatabaseAccessorFactory DatabaseAccessorFactory => this._databaseAccessorFactoryAccessor.Value;
        #endregion

        #region Constructor
        protected DatabaseTestBase()
        {
            this._databaseAccessorFactoryAccessor = new Lazy<IDisposableDatabaseAccessorFactory>(() => CreateDatabaseAccessorFactoryCore());
        }
        #endregion

        #region Protected Methods
        protected override async Task OnTestInitialized()
        {
            await base.OnTestInitialized().ConfigureAwait(false);
            ConcurrentTestDibixTraceRedirector.Register(DibixTraceSources);
        }

        protected async Task<TResult> ExecuteDatabaseAction<TResult>(Func<IDatabaseAccessor, Task<TResult>> action, Action<DatabaseAccessorOptions> configure = null)
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create(configure))
            {
                return await action(accessor).ConfigureAwait(false);
            }
        }

        protected async Task ExecuteStoredProcedure(string storedProcedureName, Action<IParameterBuilder> parameters = null, int commandTimeout = 30, Action<DatabaseAccessorOptions> configure = null)
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create(configure))
            {
                IParameterBuilder parameterBuilder = accessor.Parameters();
                parameters?.Invoke(parameterBuilder);
                await accessor.ExecuteAsync(storedProcedureName, CommandType.StoredProcedure, parameterBuilder.Build(), commandTimeout, CancellationToken.None).ConfigureAwait(false);
            }
        }

        protected IDatabaseAccessorFactory CreateDatabaseAccessorFactory(int? commandTimeout = 30) => CreateDatabaseAccessorFactoryCore(commandTimeout);
        #endregion

        #region Private Methods
        private IDisposableDatabaseAccessorFactory CreateDatabaseAccessorFactoryCore(int? commandTimeout = 30) => DatabaseTestUtility.CreateDatabaseAccessorFactory(base.Configuration, commandTimeout);
        #endregion

        #region IDisposable Members
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (ConcurrentTestTextWriter.ListenerCount == 0 && Scope != TestClassInstanceScope.AssemblyInitialize)
                ConcurrentTestDibixTraceRedirector.Unregister(DibixTraceSources);

            if (_databaseAccessorFactoryAccessor.IsValueCreated)
                _databaseAccessorFactoryAccessor.Value.Dispose();
        }
        #endregion
    }
}