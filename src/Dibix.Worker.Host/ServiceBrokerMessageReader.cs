using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Worker.Abstractions;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceBrokerMessageReader : IServiceBrokerMessageReader
    {
        private readonly ServiceScopeWorkerScopeFactory _workerScopeFactory;
        private readonly IHostedServiceEvents _hostedServiceEvents;

        public ServiceBrokerMessageReader(ServiceScopeWorkerScopeFactory workerScopeFactory, IHostedServiceEvents hostedServiceEvents)
        {
            _workerScopeFactory = workerScopeFactory;
            _hostedServiceEvents = hostedServiceEvents;
        }

        public async Task<IEnumerable<TMessage>> ReadMessages<TMessage>(string fullSubscriberName, Func<IDatabaseAccessorFactory, int, CancellationToken, Task<IEnumerable<TMessage>>> handler, CancellationToken cancellationToken)
        {
            using WorkerScope scope = _workerScopeFactory.Create(fullSubscriberName);
            IDatabaseAccessorFactory databaseAccessorFactory = scope.GetService<ServiceBrokerDatabaseAccessorFactory>();
            IEnumerable<TMessage> messages = await handler(databaseAccessorFactory, ServiceBrokerDefaults.ReceiveTimeout, cancellationToken).ConfigureAwait(false);
            await _hostedServiceEvents.OnServiceBrokerIterationCompleted(scope, cancellationToken).ConfigureAwait(false);
            return messages;
        }
    }
}