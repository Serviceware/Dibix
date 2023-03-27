using System;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerExtensionConfigurationBuilder
    {
        IWorkerExtensionConfigurationBuilder RegisterService<TService>() where TService : HostedService;
        IWorkerExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IWorkerExtensionConfigurationBuilder RegisterDependency(Type implementationType);
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name);
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name, Action<IWorkerHttpClientConfigurationBuilder> configure);
    }
}