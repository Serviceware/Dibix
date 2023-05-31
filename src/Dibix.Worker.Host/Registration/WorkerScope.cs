using System;
using System.Data.Common;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Host
{
    internal sealed class WorkerScope : IWorkerScope
    {
        private readonly ServiceProviderWorkerDependencyContext _dependencyContext;
        private readonly Action _onDispose;

        DbConnection IWorkerDependencyContext.Connection => _dependencyContext.Connection;
        IDatabaseAccessorFactory IWorkerDependencyContext.DatabaseAccessorFactory => _dependencyContext.DatabaseAccessorFactory;
        public string InitiatorFullName { get; }

        public WorkerScope(ServiceProviderWorkerDependencyContext dependencyContext, Action onDispose, string initiatorFullName)
        {
            InitiatorFullName = initiatorFullName;
            _dependencyContext = dependencyContext;
            _onDispose = onDispose;
        }

        T IWorkerDependencyContext.GetExtension<T>() => _dependencyContext.GetExtension<T>();
        T IWorkerDependencyContext.GetExtension<T>(Type implementationType) => _dependencyContext.GetExtension<T>(implementationType);
        ILogger IWorkerDependencyContext.CreateLogger(Type loggerType) => _dependencyContext.CreateLogger(loggerType);

        void IDisposable.Dispose() => _onDispose.Invoke();

        public T GetService<T>() where T : notnull => _dependencyContext.GetService<T>();
    }
}