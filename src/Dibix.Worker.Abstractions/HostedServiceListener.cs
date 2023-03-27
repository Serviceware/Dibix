using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class HostedServiceListener : HostedService, IHostedService
    {
        private readonly IHostedServiceRegistrar _hostedServiceRegistrar;

        protected HostedServiceListener(IHostedServiceRegistrar hostedServiceRegistrar, ILogger logger) : base(logger)
        {
            _hostedServiceRegistrar = hostedServiceRegistrar;
        }

        protected sealed override async Task StartServiceAsync(CancellationToken cancellationToken)
        {
            await _hostedServiceRegistrar.RegisterHostedService(GetType().FullName, cancellationToken).ConfigureAwait(false);
            await StartListenerAsync(cancellationToken).ConfigureAwait(false);
        }

        protected sealed override Task StopServiceAsync(CancellationToken cancellationToken) => StopListenerAsync(cancellationToken);

        protected abstract Task StartListenerAsync(CancellationToken cancellationToken);
        protected virtual Task StopListenerAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}