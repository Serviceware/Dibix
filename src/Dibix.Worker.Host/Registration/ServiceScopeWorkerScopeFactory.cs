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

        public IWorkerScope Create()
        {
            IServiceScope scope = _scopeFactory.CreateScope();
            IWorkerDependencyContext dependencyContext = scope.ServiceProvider.GetRequiredService<IWorkerDependencyContext>();
            return new WorkerScope(dependencyContext, scope.Dispose);
        }

        private sealed class WorkerScope : IWorkerScope
        {
            private readonly IWorkerDependencyContext _dependencyContext;
            private readonly Action _onDispose;

            DbConnection IWorkerDependencyContext.Connection => _dependencyContext.Connection;
            IDatabaseAccessorFactory IWorkerDependencyContext.DatabaseAccessorFactory => _dependencyContext.DatabaseAccessorFactory;

            public WorkerScope(IWorkerDependencyContext dependencyContext, Action onDispose)
            {
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