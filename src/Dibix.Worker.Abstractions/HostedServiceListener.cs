using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class HostedServiceListener : HostedService, IHostedService
    {
        private readonly IHostedServiceEvents _hostedServiceEvents;

        protected HostedServiceListener(IHostedServiceEvents hostedServiceEvents, ILogger logger) : base(logger)
        {
            _hostedServiceEvents = hostedServiceEvents;
        }

        protected sealed override async Task StartServiceAsync(CancellationToken cancellationToken)
        {
            await _hostedServiceEvents.OnWorkerStarted(GetType().FullName, cancellationToken).ConfigureAwait(false);
            await StartListenerAsync(cancellationToken).ConfigureAwait(false);
        }

        protected sealed override Task StopServiceAsync(CancellationToken cancellationToken) => StopListenerAsync(cancellationToken);

        protected abstract Task StartListenerAsync(CancellationToken cancellationToken);
        protected virtual Task StopListenerAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}