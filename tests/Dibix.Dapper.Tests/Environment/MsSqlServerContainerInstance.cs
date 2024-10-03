using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;

namespace Dibix.Dapper.Tests
{
    public struct MsSqlServerContainerInstance : IAsyncDisposable
    {
        public string ConnectionString { get; }
        internal IContainer Container { get; }
        public IAsyncDisposable OutputConsumer { get; }

        public MsSqlServerContainerInstance(IContainer container, IAsyncDisposable outputConsumer, string connectionString)
        {
            Container = container;
            OutputConsumer = outputConsumer;
            ConnectionString = connectionString;
        }

        public async ValueTask DisposeAsync()
        {
            await Container.DisposeAsync().ConfigureAwait(false);
            await OutputConsumer.DisposeAsync().ConfigureAwait(false);
        }
    }
}