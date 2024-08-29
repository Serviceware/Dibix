using System;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerExtensionConfigurationBuilder
    {
        IWorkerExtensionConfigurationBuilder RegisterService<TService>() where TService : HostedService;
        IWorkerExtensionConfigurationBuilder RegisterScopedDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IWorkerExtensionConfigurationBuilder RegisterScopedDependency<TInterface>(Type implementationType);
        IWorkerExtensionConfigurationBuilder RegisterScopedDependency(Type implementationType);
        IWorkerExtensionConfigurationBuilder RegisterSingletonDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IWorkerExtensionConfigurationBuilder RegisterSingletonDependency<TInterface>(Type implementationType);
        IWorkerExtensionConfigurationBuilder RegisterSingletonDependency(Type implementationType);
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name);
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name, Action<IWorkerHttpClientConfigurationBuilder> configure);
    }
}