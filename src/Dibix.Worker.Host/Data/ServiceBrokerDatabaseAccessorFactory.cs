using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dibix.Hosting.Abstractions.Data;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceBrokerDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly IDatabaseConnectionResolver _connectionResolver;
        private readonly ILogger _logger;

        public ServiceBrokerDatabaseAccessorFactory(IDatabaseConnectionResolver connectionResolver, CreateDatabaseLogger loggerFactory)
        {
            _connectionResolver = connectionResolver;
            _logger = loggerFactory();
        }

        public IDatabaseAccessor Create()
        {
            DbConnection connection = _connectionResolver.Resolve();
            return new ServiceBrokerDatabaseAccessor(connection, _logger);
        }
    }

    internal sealed class ServiceBrokerDatabaseAccessor : DatabaseAccessor
    {
        private readonly ILogger _logger;

        public ServiceBrokerDatabaseAccessor(DbConnection connection, ILogger logger) : base(connection)
        {
            _logger = logger;
        }

        protected override int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout) => throw new NotImplementedException();

        protected override Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => throw new NotImplementedException();

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered) => throw new NotImplementedException();

        // ServiceBrokerSubscriber<TMessage>
        protected override async Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken)
        {
            if (!buffered)
                throw new NotSupportedException("Non buffered results are not supported by this implementation");

            IEnumerable<T> ProcessResult(IEnumerable<T> x) => x.ToArray();
            return await QueryAsync<T, IEnumerable<T>>(commandText, commandType, parameters, ProcessResult, cancellationToken).ConfigureAwait(false);
        }

        protected override IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn) => throw new NotImplementedException();

        protected override T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => throw new NotImplementedException();

        // ServiceBrokerSignalSubscriber
        protected override async Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            T ProcessResult(IEnumerable<T> result)
            {
                using IEnumerator<T> enumerator = result.GetEnumerator();

                T current;

                if (enumerator.MoveNext())
                {
                    current = enumerator.Current;
                    if (enumerator.MoveNext())
                        throw CreateException(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, commandText, commandType, parameters);
                }
                else
                {
                    return default!; // Hmmm
                }

                return current;
            }
            return await QueryAsync<T, T>(commandText, commandType, parameters, ProcessResult, cancellationToken).ConfigureAwait(false);
        }

        protected override IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => throw new NotImplementedException();

        protected override Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override void OnInfoMessage(string message) => _logger.LogDebug($"[SQL] {message}");

        protected override void DisposeConnection()
        {
            // Disposal of the connection is responsibility of the consumer.
            // This is currently done by registering this as a scoped instance, that will be disposed after each request.
        }

        private async Task<TResult> QueryAsync<TRow, TResult>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<IEnumerable<TRow>, TResult> resultProcessor, CancellationToken cancellationToken)
        {
            using DbCommand command = Connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            command.CommandTimeout = ServiceBrokerDefaults.CommandTimeout;

            parameters.VisitInputParameters((name, type, value, isOutput, customInputType) => CollectInputParameter(command, name, type, value, isOutput, customInputType));

            using (EnterCancellationScope(cancellationToken, command))
            {
                // Normally we would use ExecuteReaderAsync(cancellationToken) here,
                // but our receive procedures are using RAISERROR WITH NOWAIT to report progress in realtime.
                // The usage of NOWAIT however seems to block the cancellation.
                // Therefore we have to use the sync method and cancel the command ourselves.
                // See: https://stackoverflow.com/questions/24738417/canceling-sql-server-query-with-cancellationtoken/24834029#24834029
                TResult result;
                using (IDataReader reader = await Task.Run(command.ExecuteReader, cancellationToken).ConfigureAwait(false))
                {
                    result = resultProcessor(reader.Parse<TRow>());
                }
                parameters.VisitOutputParameters(name => CollectOutputParameter(command, name));
                return result;
            }
        }

        private IDisposable EnterCancellationScope(CancellationToken cancellationToken, IDbCommand command)
        {
            return cancellationToken.Register(() => HandleCancellationRequest(command));
        }

        private void HandleCancellationRequest(IDbCommand command)
        {
            _logger.LogDebug("Cancelling current service broker queue read operation");
            command?.Cancel(); // this method throws a SqlException => catch needed!
        }

        private static void CollectInputParameter(DbCommand command, string name, DbType type, object value, bool isOutput, CustomInputType customInputType)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value;
            parameter.Direction = isOutput ? ParameterDirection.Output : ParameterDirection.Input;
            command.Parameters.Add(parameter);
        }

        private static object? CollectOutputParameter(DbCommand command, string name) => command.Parameters[name].Value;
    }
}