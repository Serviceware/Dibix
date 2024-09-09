using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerHostExtensionConfigurationBuilder : IWorkerConfigurationBuilder<IWorkerHostExtensionConfigurationBuilder>
    {
        IWorkerHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string?, string?> configure);
        IWorkerHostExtensionConfigurationBuilder OnHostStarted(Func<IWorkerDependencyContext, Task> handler);
        IWorkerHostExtensionConfigurationBuilder OnHostStopped(Func<IWorkerDependencyContext, Task> handler);
        IWorkerHostExtensionConfigurationBuilder OnWorkerStarted(OnWorkerStarted handler);
        IWorkerHostExtensionConfigurationBuilder OnServiceBrokerIterationCompleted(OnServiceBrokerIterationCompleted handler);
    }

    public delegate Task OnWorkerStarted(IWorkerDependencyContext dependencyContext, CancellationToken cancellationToken);
    
    public delegate Task OnServiceBrokerIterationCompleted(IWorkerDependencyContext dependencyContext, CancellationToken cancellationToken);
}