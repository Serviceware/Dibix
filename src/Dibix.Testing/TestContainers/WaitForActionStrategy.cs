using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Dibix.Testing.TestContainers
{
    public sealed class WaitForActionStrategy : IWaitUntil
    {
        private readonly Func<IContainer, Task<bool>> _test;

        public WaitForActionStrategy(Func<IContainer, Task<bool>> test) => _test = test;

        public Task<bool> UntilAsync(IContainer container) => _test(container);
    }
}