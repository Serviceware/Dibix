using System;
using System.Threading.Tasks;
using Dibix.Testing.Data;

namespace Dibix.Dapper.Tests
{
    public abstract class DapperTestBase : DatabaseTestBase<TestConfiguration>
    {
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