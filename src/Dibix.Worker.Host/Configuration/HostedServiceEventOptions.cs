using Dibix.Worker.Abstractions;

namespace Dibix.Worker.Host
{
    public sealed class HostedServiceEventOptions
    {
        internal OnWorkerStarted? OnWorkerStarted { get; set; }
        internal OnServiceBrokerIterationCompleted? OnServiceBrokerIterationCompleted { get; set; }
    }
}