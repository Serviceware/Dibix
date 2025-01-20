using System.Data.Common;
using Dibix.Dapper;
using Microsoft.Extensions.Logging;

namespace Dibix.Hosting.Abstractions.Data
{
    internal sealed class ScopedDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly IDatabaseConnectionResolver _connectionResolver;
        private readonly ILogger _logger;

        public ScopedDatabaseAccessorFactory(IDatabaseConnectionResolver connectionResolver, CreateDatabaseLogger loggerFactory)
        {
            _connectionResolver = connectionResolver;
            _logger = loggerFactory();
        }

        public IDatabaseAccessor Create()
        {
            DbConnection connection = _connectionResolver.Resolve();
            return new LoggingDapperDatabaseAccessor(connection, _logger);
        }

        private sealed class LoggingDapperDatabaseAccessor : DapperDatabaseAccessor
        {
            private readonly ILogger _logger;

            public LoggingDapperDatabaseAccessor(DbConnection connection, ILogger logger) : base(connection)
            {
                _logger = logger;
            }

            protected override void OnInfoMessage(string message) => _logger.LogDebug($"[SQL] {message}");

            protected override void DisposeConnection()
            {
                // Disposal of the connection is responsibility of the consumer.
                // This is currently done by registering this as a scoped instance, that will be disposed after each request.
            }
        }
    }

    internal delegate ILogger CreateDatabaseLogger();
}