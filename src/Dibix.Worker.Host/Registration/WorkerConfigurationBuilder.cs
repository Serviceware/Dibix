using System;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Worker.Host
{
    internal abstract class WorkerConfigurationBuilder<TBuilder> : IWorkerConfigurationBuilder<TBuilder> where TBuilder : IWorkerConfigurationBuilder<TBuilder>
    {
        private readonly IServiceCollection _services;
        private readonly WorkerDependencyRegistry _dependencyRegistry;

        protected abstract TBuilder This { get; }

        protected WorkerConfigurationBuilder(IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
        {
            _services = services;
            _dependencyRegistry = dependencyRegistry;
        }

        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterService<TService>()
        {
            _services.AddHostedService<TService>();
            return This;
        }

        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterScopedDependency<TInterface, TImplementation>()
        {
            _services.AddScopedOnce<TInterface, TImplementation>();
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterScopedDependency<TInterface>(Type implementationType)
        {
            _services.AddScopedOnce<TInterface>(implementationType);
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterScopedDependency(Type implementationType)
        {
            _services.AddScopedOnce(implementationType);
            _dependencyRegistry.Register(implementationType);
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterScopedDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory)
        {
            TInterface CreateInstance(IServiceProvider serviceProvider)
            {
                IWorkerDependencyContext dependencyContext = serviceProvider.GetRequiredService<IWorkerDependencyContext>();
                return factory(dependencyContext);
            }
            _services.AddScopedOnce(CreateInstance);
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }

        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterSingletonDependency<TInterface, TImplementation>()
        {
            _services.AddSingletonOnce<TInterface, TImplementation>();
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterSingletonDependency<TInterface>(Type implementationType)
        {
            _services.AddSingletonOnce<TInterface>(implementationType);
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterSingletonDependency(Type implementationType)
        {
            _services.AddSingletonOnce(implementationType);
            _dependencyRegistry.Register(implementationType);
            return This;
        }
        TBuilder IWorkerConfigurationBuilder<TBuilder>.RegisterSingletonDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory)
        {
            TInterface CreateInstance(IServiceProvider serviceProvider)
            {
                IWorkerDependencyContext dependencyContext = serviceProvider.GetRequiredService<IWorkerDependencyContext>();
                return factory(dependencyContext);
            }
            _services.AddSingletonOnce(CreateInstance);
            _dependencyRegistry.Register(typeof(TInterface));
            return This;
        }
    }
}