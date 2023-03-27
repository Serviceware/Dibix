using System.Threading;
using System.Threading.Tasks;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.Options;

namespace Dibix.Worker.Host
{
    internal sealed class DefaultHostedServiceRegistrar : IHostedServiceRegistrar
    {
        private readonly IWorkerScopeFactory _scopeFactory;
        private readonly IOptions<HostedServiceRegistrarOptions> _options;

        public DefaultHostedServiceRegistrar(IWorkerScopeFactory scopeFactory, IOptions<HostedServiceRegistrarOptions> options)
        {
            _scopeFactory = scopeFactory;
            _options = options;
        }

        public async Task RegisterHostedService(string fullName, CancellationToken cancellationToken)
        {
            if (_options.Value.Handler == null)
                return;

            using IWorkerScope scope = _scopeFactory.Create();
            await _options.Value.Handler(fullName, scope, cancellationToken).ConfigureAwait(false);
        }
    }
}