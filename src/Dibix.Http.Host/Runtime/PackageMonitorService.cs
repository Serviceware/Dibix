using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal sealed class PackageMonitorService : IHostedService, IDisposable
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<PackageMonitorService> _logger;
        private readonly FileSystemWatcher _watcher;

        public PackageMonitorService(IHostApplicationLifetime hostApplicationLifetime, ILogger<PackageMonitorService> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _watcher = new FileSystemWatcher
            {
                Path = ApplicationEnvironment.PackagesDirectory,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = $"*.{ApplicationEnvironment.PackageExtension}",
                IncludeSubdirectories = false
            };
            _watcher.Changed += OnFileChanged;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Start monitoring for '*.{packageExtension}' changes in '{packagesDirectory}'", ApplicationEnvironment.PackageExtension, ApplicationEnvironment.PackagesDirectory);
            _watcher.EnableRaisingEvents = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stop monitoring for '*.{packageExtension}' changes in '{packagesDirectory}'", ApplicationEnvironment.PackageExtension, ApplicationEnvironment.PackagesDirectory);
            _watcher.EnableRaisingEvents = false;
            return Task.CompletedTask;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed || e.Name == null)
                return;

            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation("Detected file change in packages directory: {fileName} [{changeType}]", e.Name, e.ChangeType);
            _logger.LogInformation("Triggering application shutdown");

            _hostApplicationLifetime.StopApplication();
        }

        void IDisposable.Dispose()
        {
            _watcher.Changed -= OnFileChanged;
            _watcher?.Dispose();
        }
    }
}