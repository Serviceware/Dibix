using System;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Worker.Host
{
    internal sealed class DatabaseOptionsMonitor : BackgroundService
    {
        private readonly ILogger<DatabaseOptionsMonitor> _logger;
        private readonly IOptionsMonitor<DatabaseOptions> _databaseOptions;
        private readonly IDisposable? _configurationChangedToken;

        public DatabaseOptionsMonitor(ILogger<DatabaseOptionsMonitor> logger, IOptionsMonitor<DatabaseOptions> databaseOptions)
        {
            _logger = logger;
            _databaseOptions = databaseOptions;
            _configurationChangedToken = databaseOptions.OnChange(OnDatabaseOptionsChanged);
        }

        public override void Dispose()
        {
            base.Dispose();

            _configurationChangedToken?.Dispose();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Database connection string: {connectionString}", _databaseOptions.CurrentValue.ConnectionString);
            return Task.CompletedTask;
        }

        private void OnDatabaseOptionsChanged(DatabaseOptions options, string? name)
        {
            const string message = "Database connection string changed";
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"{message}: {{connectionString}}", options.ConnectionString);
            else
                _logger.LogInformation("Database connection string changed");
        }
    }
}