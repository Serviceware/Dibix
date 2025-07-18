using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class DatabaseAccessor : IDatabaseAccessor, IDisposable
    {
        #region Properties
        protected DbConnection Connection { get; }
        protected DbProviderAdapter DbProviderAdapter { get; }
        #endregion

        #region Constructor
        protected DatabaseAccessor(DbConnection connection)
        {
            Guard.IsNotNull(connection, nameof(connection));

            Connection = connection;
            DbProviderAdapter = DbProviderAdapterRegistry.Get(connection);

            DbProviderAdapter.AttachInfoMessageHandler(OnInfoMessageEvent);
        }
        #endregion

        #region IDatabaseAccessor Members
        public IParameterBuilder Parameters() => new ParameterBuilder();

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => Execute(commandText, commandType, parameters, commandTimeout: null));

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout) => Execute(commandText, commandType, parameters, () => Execute(commandText, commandType, parameters, commandTimeout));

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => ExecuteAsync(commandText, commandType, parameters, commandTimeout: null, cancellationToken));

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => ExecuteAsync(commandText, commandType, parameters, commandTimeout, cancellationToken));

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QueryMany<T>(commandText, commandType, parameters).PostProcess());

        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryManyAsync<T>(commandText, commandType, parameters, buffered: true, cancellationToken).PostProcess());

        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryManyAsync<T>(commandText, commandType, parameters, buffered, cancellationToken).PostProcess());

        public IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QueryManyAutoMultiMap<TReturn>(commandText, commandType, parameters, types, splitOn));

        public Task<IEnumerable<TReturn>> QueryManyAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Execute(commandText, commandType, parameters, () => QueryManyAutoMultiMapAsync<TReturn>(commandText, commandType, parameters, types, splitOn, cancellationToken));

        T IDatabaseAccessor.QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: false));

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: false, cancellationToken));

        public TReturn QuerySingle<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingle<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: false));

        public Task<TReturn> QuerySingleAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingleAsync<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: false, cancellationToken));

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: true));

        Task<T> IDatabaseAccessor.QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: true, cancellationToken));

        public TReturn QuerySingleOrDefault<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingle<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: true));

        public Task<TReturn> QuerySingleOrDefaultAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingleAsync<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: true, cancellationToken));

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QueryMultiple(commandText, commandType, parameters));

        Task<IMultipleResultReader> IDatabaseAccessor.QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryMultipleAsync(commandText, commandType, parameters, cancellationToken));
        #endregion

        #region Abstract Methods
        protected abstract int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout);

        protected abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken);

        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters);

        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered);

        protected abstract Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);

        protected abstract IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered);

        protected abstract Task<IEnumerable<TReturn>> QueryManyAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered, CancellationToken cancellationToken);

        protected abstract IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters);

        protected abstract Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        #endregion

        #region Protected Methods
        protected virtual void DisposeConnection() => Connection?.Dispose();

        protected virtual void OnInfoMessage(string message) { }
        #endregion

        #region Private Methods
        private IEnumerable<TReturn> QueryManyAutoMultiMap<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool buffered = true) where TReturn : new()
        {
            ValidateMultiMapSegments(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return QueryMany(commandText, commandType, parameters, types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered).PostProcess(multiMapper);
        }
        private Task<IEnumerable<TReturn>> QueryManyAutoMultiMapAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken, bool buffered = true) where TReturn : new()
        {
            ValidateMultiMapSegments(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return QueryManyAsync(commandText, commandType, parameters, types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered, cancellationToken).PostProcess(multiMapper);
        }

        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty)
        {
            IEnumerable<T> result = QueryMany<T>(commandText, commandType, parameters, buffered: false).PostProcess();
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }
        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = QueryManyAutoMultiMap<T>(commandText, commandType, parameters, types, splitOn, buffered: false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool defaultIfEmpty, CancellationToken cancellationToken) where T : new()
        {
            IEnumerable<T> result = await QueryManyAutoMultiMapAsync<T>(commandText, commandType, parameters, types, splitOn, cancellationToken, buffered: false).ConfigureAwait(false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty, CancellationToken cancellationToken)
        {
            IEnumerable<T> result = await QueryManyAsync<T>(commandText, commandType, parameters, buffered: false, cancellationToken).PostProcess().ConfigureAwait(false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }

        private T Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private async Task<T> Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<Task<T>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException && databaseAccessException.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception, DbProviderAdapter.TryGetSqlErrorNumber(exception.InnerException), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }

        private void OnInfoMessageEvent(string message)
        {
            DibixTraceSource.Sql.TraceInformation(message);
            OnInfoMessage(message);
        }

        private static void ValidateMultiMapSegments(IReadOnlyCollection<Type> types, string splitOn)
        {
            MultiMapUtility.ValidateParameters(types, splitOn);
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DbProviderAdapter.DetachInfoMessageHandler();
                DisposeConnection();
            }
        }
        #endregion
    }
}