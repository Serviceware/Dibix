using System;
using System.Data.Common;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceProviderWorkerDependencyContext : IWorkerDependencyContext
    {
        private readonly IWorkerDependencyRegistry _dependencyRegistry;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<DbConnection> _connectionAccessor;
        private readonly Lazy<IDatabaseAccessorFactory> _databaseAccessorFactoryAccessor;
        private string? _initiatorFullName;

        public DbConnection Connection => _connectionAccessor.Value;
        public IDatabaseAccessorFactory DatabaseAccessorFactory => _databaseAccessorFactoryAccessor.Value;
        public string InitiatorFullName
        {
            get
            {
                if (_initiatorFullName == null)
                    throw new InvalidOperationException($"{nameof(InitiatorFullName)} property not initialized");

                return _initiatorFullName;
            }
            set => _initiatorFullName = value;
        }

        public ServiceProviderWorkerDependencyContext(IWorkerDependencyRegistry dependencyRegistry, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _dependencyRegistry = dependencyRegistry;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _connectionAccessor = new Lazy<DbConnection>(serviceProvider.GetRequiredService<DbConnection>);
            _databaseAccessorFactoryAccessor = new Lazy<IDatabaseAccessorFactory>(serviceProvider.GetRequiredService<IDatabaseAccessorFactory>);
        }

        public T GetExtension<T>() where T : notnull
        {
            VerifyExtensionRegistered(typeof(T));
            return _serviceProvider.GetRequiredService<T>();
        }

        public T GetExtension<T>(Type implementationType) where T : notnull
        {
            VerifyExtensionRegistered(implementationType);
            return (T)_serviceProvider.GetRequiredService(implementationType);
        }

        public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

        public ILogger CreateLogger(Type loggerType) => _loggerFactory.CreateLogger(loggerType);

        private void VerifyExtensionRegistered(Type type)
        {
            if (!_dependencyRegistry.IsRegistered(type))
                throw new InvalidOperationException($"Extension not registered: {type}");
        }
    }
}