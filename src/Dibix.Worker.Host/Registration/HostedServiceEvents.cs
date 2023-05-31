using System.Threading;
using System.Threading.Tasks;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.Options;

namespace Dibix.Worker.Host
{
    internal sealed class HostedServiceEvents : IHostedServiceEvents
    {
        private readonly IWorkerScopeFactory _scopeFactory;
        private readonly IOptions<HostedServiceEventOptions> _options;

        public HostedServiceEvents(IWorkerScopeFactory scopeFactory, IOptions<HostedServiceEventOptions> options)
        {
            _scopeFactory = scopeFactory;
            _options = options;
        }

        public async Task OnWorkerStarted(string fullName, CancellationToken cancellationToken)
        {
            if (_options.Value.OnWorkerStarted == null)
                return;

            using IWorkerScope scope = _scopeFactory.Create(fullName);
            await _options.Value.OnWorkerStarted(scope, cancellationToken).ConfigureAwait(false);
        }

        public async Task OnServiceBrokerIterationCompleted(IWorkerScope scope, CancellationToken cancellationToken)
        {
            if (_options.Value.OnServiceBrokerIterationCompleted == null)
                return;

            await _options.Value.OnServiceBrokerIterationCompleted(scope, cancellationToken).ConfigureAwait(false);
        }
    }
}