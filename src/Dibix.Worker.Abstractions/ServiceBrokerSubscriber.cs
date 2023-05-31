using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class ServiceBrokerSubscriber<TMessage> : BackgroundService, IHostedService
    {
        #region Fields
        private const int RetryOnErrorDelay = 10000; // ms
        private readonly IServiceBrokerMessageReader _serviceBrokerMessageReader;
        private readonly string _fullSubscriberName;
        #endregion

        #region Constructor
        protected ServiceBrokerSubscriber(IServiceBrokerMessageReader serviceBrokerMessageReader, IHostedServiceEvents hostedServiceEvents, ILogger logger) : base(hostedServiceEvents, logger)
        {
            _serviceBrokerMessageReader = serviceBrokerMessageReader;
            _fullSubscriberName = GetType().FullName;
        }
        #endregion

        #region Protected Methods
        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogDebug("Service broker queue receive loop entered");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ICollection<TMessage> messages = await ReceiveMessageBatch(stoppingToken).ConfigureAwait(false);
                    if (messages.Any())
                        _ = ProcessMessages(messages);
                }
                catch (Exception exception) when (ExceptionUtility.IsCancellationException(exception, stoppingToken))
                {
                    Logger.LogDebug("Service broker queue receive operation was cancelled");
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "An error occurred while reading service broker messages");
                    try
                    {
                        await Task.Delay(RetryOnErrorDelay, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        Logger.LogDebug("Service broker queue receive operation was cancelled during retry on error");
                    }
                }
            }
        }

        protected abstract Task<IEnumerable<TMessage>> ReceiveMessages(IDatabaseAccessorFactory databaseAccessorFactory, int receiveTimeout, CancellationToken cancellationToken);

        protected abstract Task ProcessMessage(TMessage message);

        private protected virtual async Task ProcessMessages(IEnumerable<TMessage> messages)
        {
            foreach (TMessage message in messages)
            {
                try
                {
                    await ProcessMessage(message).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "An error occured while processing service broker message");
                }
            }
        }
        #endregion

        #region Private Methods
        private async Task<ICollection<TMessage>> ReceiveMessageBatch(CancellationToken cancellationToken)
        {
            return (await _serviceBrokerMessageReader.ReadMessages(_fullSubscriberName, ReceiveMessages, cancellationToken).ConfigureAwait(false)).ToArray();
        }
        #endregion
    }
}