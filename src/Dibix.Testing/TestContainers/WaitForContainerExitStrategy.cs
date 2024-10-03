using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Dibix.Testing.TestContainers
{
    public sealed class WaitForContainerExitStrategy : IWaitUntil
    {
        public Task<bool> UntilAsync(IContainer container) => Task.FromResult(container.State == TestcontainersStates.Exited);
    }
}