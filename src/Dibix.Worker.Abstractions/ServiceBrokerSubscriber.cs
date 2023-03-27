using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
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
                        ProcessMessages(messages);
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

        private protected virtual void ProcessMessages(IEnumerable<TMessage> messages)
        {
            foreach (TMessage message in messages)
                Task.Run(() => ProcessMessage(message));
        }
        #endregion

        #region Private Methods
        private async Task<ICollection<TMessage>> ReceiveMessageBatch(CancellationToken cancellationToken)
        {
            using (IWorkerScope scope = _scopeFactory.Create())
            {
                SqlException? sqlException = null;
                if (scope.Connection is SqlConnection sqlConnection)
                {
                    // Important for tracing when using RAISERROR WITH NOWAIT
                    // Caution:
                    // This also means that errors will not throw an exception,
                    // and will instead trigger an InfoMessage event aswell.
                    // Therefore we will throw them ourselves.
                    sqlConnection.FireInfoMessageEventOnUserErrors = true;
                    sqlConnection.InfoMessage += (_, e) => OnInfoMessage(e, ref sqlException);
                }

                using (DbCommand command = scope.Connection.CreateCommand())
                {
                    command.CommandText = ReceiveProcedureName;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = CommandTimeout;
                    AddParameter(command, "timeout", DbType.Int32, ReceiveTimeout);

                    // Normally we would use ExecuteReaderAsync(cancellationToken) here,
                    // but our receive procedures are using RAISERROR WITH NOWAIT to report progress in realtime.
                    // The usage of NOWAIT however seems to block the cancellation.
                    // Therefore we have to use the sync method and cancel the command ourselves.
                    // See: https://stackoverflow.com/questions/24738417/canceling-sql-server-query-with-cancellationtoken/24834029#24834029
                    using (EnterCancellationScope(cancellationToken, command))
                    {
                        DbDataReader ExecuteReader()
                        {
                            DbDataReader reader = command.ExecuteReader();
                            if (sqlException != null)
                                throw sqlException;

                            return reader;
                        }

                        using (IDataReader reader = await Task.Run(ExecuteReader, cancellationToken).ConfigureAwait(false))
                        {
                            return reader.Parse<TMessage>().ToArray();
                        }
                    }

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

        private void OnInfoMessage(SqlInfoMessageEventArgs args, ref SqlException? sqlException)
        {
            bool isError = args.Errors.Cast<SqlError>().Aggregate(false, (current, sqlError) => current || sqlError.Class > 10);
            if (!isError)
            {
                Logger.LogDebug(args.Message);
                return;
            }
            sqlException = SqlExceptionFactory.Create(args.Errors);
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

        #region Nested Types
        private static class SqlExceptionFactory
        {
            private static readonly Func<SqlErrorCollection, SqlException> CompiledMethod = CompileSqlExceptionFactory();

            public static SqlException Create(SqlErrorCollection errors) => CompiledMethod(errors);

            private static Func<SqlErrorCollection, SqlException> CompileSqlExceptionFactory()
            {
                // (SqlErrorCollection errorCollection) => 
                ParameterExpression errorCollectionParameter = Expression.Parameter(typeof(SqlErrorCollection), "errorCollectionParameter");

                // SqlException.CreateException(errorCollection, null);
                Expression serverVersion = Expression.Constant(null, typeof(string));
                Expression createExceptionCall = Expression.Call(typeof(SqlException), "CreateException", Type.EmptyTypes, errorCollectionParameter, serverVersion);

                Expression<Func<SqlErrorCollection, SqlException>> lambda = Expression.Lambda<Func<SqlErrorCollection, SqlException>>(createExceptionCall, errorCollectionParameter);
                Func<SqlErrorCollection, SqlException> compiled = lambda.Compile();
                return compiled;
            }
        }
        #endregion
    }
}