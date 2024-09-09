using System;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerConfigurationBuilder<out TBuilder>
    {
        TBuilder RegisterService<TService>() where TService : HostedService;
        TBuilder RegisterScopedDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        TBuilder RegisterScopedDependency<TInterface>(Type implementationType);
        TBuilder RegisterScopedDependency(Type implementationType);
        TBuilder RegisterScopedDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory) where TInterface : class;
        TBuilder RegisterSingletonDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        TBuilder RegisterSingletonDependency<TInterface>(Type implementationType);
        TBuilder RegisterSingletonDependency(Type implementationType);
        TBuilder RegisterSingletonDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory) where TInterface : class;
    }
}