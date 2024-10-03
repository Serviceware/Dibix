using System;
using System.Threading.Tasks;
using Dibix.Testing.Data;

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

        protected Task ExecuteTest(Action<IDatabaseAccessor> action) => ExecuteTest(x =>
        {
            action(x);
            return Task.CompletedTask;
        });
        protected async Task ExecuteTest(Func<IDatabaseAccessor, Task> action)
        {
            using (IDatabaseAccessor accessor = base.DatabaseAccessorFactory.Create())
            {
                await action(accessor).ConfigureAwait(false);
            }
        }
    }
}