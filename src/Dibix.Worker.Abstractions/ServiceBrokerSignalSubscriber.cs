using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class ServiceBrokerSignalSubscriber : ServiceBrokerSubscriber<object>, IHostedService
    {
        protected ServiceBrokerSignalSubscriber(IServiceBrokerMessageReader serviceBrokerMessageReader, IHostedServiceEvents hostedServiceEvents, ILogger logger) : base(serviceBrokerMessageReader, hostedServiceEvents, logger) { }

        protected sealed override async Task<IEnumerable<object>> ReceiveMessages(IDatabaseAccessorFactory databaseAccessorFactory, int receiveTimeout, CancellationToken cancellationToken)
        {
            bool result = await ReceiveMessageSignal(databaseAccessorFactory, receiveTimeout, cancellationToken).ConfigureAwait(false);
            return Enumerable.Repeat(result, 1)
                             .Where(x => x)
                             .Cast<object>();
        }

        protected abstract Task<bool> ReceiveMessageSignal(IDatabaseAccessorFactory databaseAccessorFactory, int receiveTimeout, CancellationToken cancellationToken);

        private protected sealed override Task ProcessMessages(IEnumerable<object> messages) => OnMessage();

        protected sealed override Task ProcessMessage(object message) => throw new NotSupportedException();

        protected abstract Task OnMessage();
    }
}