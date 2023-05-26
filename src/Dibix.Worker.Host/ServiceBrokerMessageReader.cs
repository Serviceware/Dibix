using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceBrokerMessageReader : IServiceBrokerMessageReader
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceBrokerMessageReader(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<IEnumerable<TMessage>> ReadMessages<TMessage>(string fullSubscriberName, Func<IDatabaseAccessorFactory, int, CancellationToken, Task<IEnumerable<TMessage>>> handler, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ServiceProviderWorkerDependencyContext dependencyContext = scope.ServiceProvider.GetRequiredService<ServiceProviderWorkerDependencyContext>();
            dependencyContext.InitiatorFullName = fullSubscriberName;
            IDatabaseAccessorFactory databaseAccessorFactory = dependencyContext.GetService<ServiceBrokerDatabaseAccessorFactory>();
            return await handler(databaseAccessorFactory, ServiceBrokerDefaults.ReceiveTimeout, cancellationToken).ConfigureAwait(false);
        }
    }
}