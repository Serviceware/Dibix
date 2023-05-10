using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerHostExtensionConfigurationBuilder
    {
        IWorkerHostExtensionConfigurationBuilder RegisterService<TService>() where TService : HostedService;
        IWorkerHostExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IWorkerHostExtensionConfigurationBuilder RegisterDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory) where TInterface : class;
        IWorkerHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string?, string?> configure);
        IWorkerHostExtensionConfigurationBuilder OnHostStarted(Func<IWorkerDependencyContext, Task> handler);
        IWorkerHostExtensionConfigurationBuilder OnHostStopped(Func<IWorkerDependencyContext, Task> handler);
        IWorkerHostExtensionConfigurationBuilder OnWorkerRegistered(OnWorkerRegistered handler);
    }

    public delegate Task OnWorkerRegistered(string implementationName, IWorkerDependencyContext dependencyContext, CancellationToken cancellationToken);
}