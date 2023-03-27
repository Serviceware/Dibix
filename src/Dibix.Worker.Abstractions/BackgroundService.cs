using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class BackgroundService : HostedServiceListener, IHostedService, IDisposable
    {
        private Task? _executingTask;
        private CancellationTokenSource? _stoppingCts;

        protected BackgroundService(IHostedServiceRegistrar hostedServiceRegistrar, ILogger logger) : base(hostedServiceRegistrar, logger) { }

        protected sealed override Task StartListenerAsync(CancellationToken cancellationToken)
        {
            // Create linked token to allow cancelling executing task from provided token
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsyncCore(_stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        protected sealed override Task StopListenerAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
                return Task.CompletedTask;

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts?.Cancel();
                return Task.CompletedTask;
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                Task.WaitAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stoppingCts?.Dispose();
            }
        }

        private async Task ExecuteAsyncCore(CancellationToken stoppingToken)
        {
            try
            {
                await ExecuteAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Hosted service execution failed");
            }
        }
    }
}