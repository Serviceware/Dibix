using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class HostedService : IHostedService
    {
        private bool _isRunning;

        protected ILogger Logger { get; }

        protected HostedService(ILogger logger)
        {
            Logger = logger;
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                return;

            try
            {
                await StartServiceAsync(cancellationToken).ConfigureAwait(false);
                _isRunning = true;
            }
            catch (Exception exception) when (ExceptionUtility.IsCancellationException(exception, cancellationToken)) { }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Hosted service could not be started");
            }
        }

        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning)
                return;

            try
            {
                await StopServiceAsync(cancellationToken).ConfigureAwait(false);
                _isRunning = false;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Hosted service could not be stopped");
            }
        }
        
        protected abstract Task StartServiceAsync(CancellationToken cancellationToken);

        protected virtual Task StopServiceAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}