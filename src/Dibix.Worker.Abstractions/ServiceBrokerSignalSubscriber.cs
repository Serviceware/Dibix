using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class ServiceBrokerSignalSubscriber : ServiceBrokerSubscriber<object>, IHostedService
    {
        protected ServiceBrokerSignalSubscriber(IWorkerScopeFactory scopeFactory, IHostedServiceRegistrar hostedServiceRegistrar, ILogger logger) : base(scopeFactory, hostedServiceRegistrar, logger) { }

        private protected sealed override Task ProcessMessages(IEnumerable<object> messages) => OnMessage();

        protected sealed override Task ProcessMessage(object message) => throw new NotSupportedException();
        
        protected abstract Task OnMessage();
    }
}