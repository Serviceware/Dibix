using Dibix.Worker.Abstractions;

namespace Dibix.Worker.Host
{
    internal sealed class HostedServiceRegistrarOptions
    {
        public OnWorkerRegistered? Handler { get; set; }
    }
}