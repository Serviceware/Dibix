using System;
using System.Threading.Tasks;
using Dibix.Testing.Data;
using Dibix.Tests;

namespace Dibix.Dapper.Tests
{
    public abstract class DapperTestBase : DatabaseTestBase<TestConfiguration>
    {
        protected override async Task OnTestInitialized()
        {
            await base.OnTestInitialized().ConfigureAwait(false);
            OnContainerServicesInitialized();
        }

        private void OnContainerServicesInitialized()
        {
            if (!ContainerServices.IsInitialized)
                return;

            MsSqlServerContainerInstance msSqlServer = ContainerServices.Instance.MsSqlServer;
            Configuration.Database.ConnectionString = msSqlServer.ConnectionString;
        }

        protected Task ExecuteTest(Action<IDatabaseAccessor> action, Action<DatabaseAccessorOptions>? configure = null) => ExecuteTest(x =>
        {
            action(x);
            return Task.CompletedTask;
        }, configure);
        protected async Task ExecuteTest(Func<IDatabaseAccessor, Task> action, Action<DatabaseAccessorOptions>? configure = null)
        {
            using (IDatabaseAccessor accessor = base.DatabaseAccessorFactory.Create(configure))
            {
                await action(accessor).ConfigureAwait(false);
            }
        }
    }
}