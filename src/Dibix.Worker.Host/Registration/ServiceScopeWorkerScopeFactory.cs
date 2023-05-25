using System;
using System.Data.Common;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceScopeWorkerScopeFactory : IWorkerScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceScopeWorkerScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IWorkerScope Create<TInitiator>() => Create(typeof(TInitiator).FullName!);
        public IWorkerScope Create(string initiatorFullName)
        {
            IServiceScope scope = _scopeFactory.CreateScope();
            ServiceProviderWorkerDependencyContext dependencyContext = scope.ServiceProvider.GetRequiredService<ServiceProviderWorkerDependencyContext>();
            dependencyContext.InitiatorFullName = initiatorFullName;
            return new WorkerScope(dependencyContext, scope.Dispose, initiatorFullName);
        }

        private sealed class WorkerScope : IWorkerScope
        {
            private readonly IWorkerDependencyContext _dependencyContext;
            private readonly Action _onDispose;

            DbConnection IWorkerDependencyContext.Connection => _dependencyContext.Connection;
            IDatabaseAccessorFactory IWorkerDependencyContext.DatabaseAccessorFactory => _dependencyContext.DatabaseAccessorFactory;
            public string InitiatorFullName { get; }

            public WorkerScope(IWorkerDependencyContext dependencyContext, Action onDispose, string initiatorFullName)
            {
                InitiatorFullName = initiatorFullName;
                _dependencyContext = dependencyContext;
                _onDispose = onDispose;
            }

            T IWorkerDependencyContext.GetExtension<T>() => _dependencyContext.GetExtension<T>();
            T IWorkerDependencyContext.GetExtension<T>(Type implementationType) => _dependencyContext.GetExtension<T>(implementationType);
            ILogger IWorkerDependencyContext.CreateLogger(Type loggerType) => _dependencyContext.CreateLogger(loggerType);

            void IDisposable.Dispose() => _onDispose.Invoke();
        }
    }
}