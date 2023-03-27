using System;
using System.Data.Common;
using Dibix.Dapper;
using Microsoft.Extensions.Logging;

namespace Dibix.Hosting.Abstractions.Data
{
    internal sealed class ScopedDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly IDatabaseConnectionResolver _connectionResolver;
        private readonly ILoggerFactory _loggerFactory;

        public ScopedDatabaseAccessorFactory(IDatabaseConnectionResolver connectionResolver, ILoggerFactory loggerFactory)
        {
            _connectionResolver = connectionResolver;
            _loggerFactory = loggerFactory;
        }

        public IDatabaseAccessor Create()
        {
            DbConnection connection = _connectionResolver.Resolve();
            return new LoggingDapperDatabaseAccessor(connection, _loggerFactory, onDispose: () =>
            {
                // Disposal of the connection is responsibility of the consumer.
                // This is currently done by registering this as a scoped instance, that will be disposed after each request.
            });
        }

        private sealed class LoggingDapperDatabaseAccessor : DapperDatabaseAccessor
        {
            private readonly ILogger _logger;

            public LoggingDapperDatabaseAccessor(DbConnection connection, ILoggerFactory loggerFactory, Action onDispose) : base(connection, onDispose: onDispose)
            {
                _logger = loggerFactory.CreateLogger("Dibix.Sql");
            }

            protected override void OnInfoMessage(string message) => _logger.LogDebug(message);
        }
    }
}