﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public abstract class ServiceBrokerSubscriber<TMessage> : BackgroundService, IHostedService
    {
        #region Fields
        private const int CommandTimeout    = 60;                        // seconds
        private const int ReceiveTimeout    = CommandTimeout / 2 * 1000; // ms
        private const int RetryOnErrorDelay = 10000;                     // ms
        private readonly IWorkerScopeFactory _scopeFactory;
        #endregion

        #region Properties
        protected abstract string ReceiveProcedureName { get; }
        #endregion

        #region Constructor
        protected ServiceBrokerSubscriber(IWorkerScopeFactory scopeFactory, IHostedServiceRegistrar hostedServiceRegistrar, ILogger logger) : base(hostedServiceRegistrar, logger)
        {
            _scopeFactory = scopeFactory;
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
            using IWorkerScope scope = _scopeFactory.Create();
            
            if (scope.Connection is SqlConnection sqlConnection)
            {
                void OnInfoMessage(object _, SqlInfoMessageEventArgs e) => Logger.LogDebug($"[SQL] {e.Message}");
                sqlConnection.InfoMessage += OnInfoMessage;
            }

            using DbCommand command = scope.Connection.CreateCommand();
            command.CommandText = ReceiveProcedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = CommandTimeout;
            AddParameter(command, "timeout", DbType.Int32, ReceiveTimeout);

            using (EnterCancellationScope(cancellationToken, command))
            {
                // Normally we would use ExecuteReaderAsync(cancellationToken) here,
                // but our receive procedures are using RAISERROR WITH NOWAIT to report progress in realtime.
                // The usage of NOWAIT however seems to block the cancellation.
                // Therefore we have to use the sync method and cancel the command ourselves.
                // See: https://stackoverflow.com/questions/24738417/canceling-sql-server-query-with-cancellationtoken/24834029#24834029
                using (IDataReader reader = await Task.Run(command.ExecuteReader, cancellationToken).ConfigureAwait(false))
                {
                    return reader.Parse<TMessage>().ToArray();
                }
            }
        }

        private IDisposable EnterCancellationScope(CancellationToken cancellationToken, IDbCommand command)
        {
            return cancellationToken.Register(() => HandleCancellationRequest(command));
        }

        private void HandleCancellationRequest(IDbCommand command)
        {
            Logger.LogDebug("Cancelling current service broker queue read operation");
            command?.Cancel(); // this method throws a SqlException => catch needed!
        }

        private static void AddParameter(IDbCommand command, string parameterName, DbType parameterType, object value)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = parameterName;
            param.DbType = parameterType;
            param.Value = value;
            command.Parameters.Add(param);
        }
        #endregion
    }
}