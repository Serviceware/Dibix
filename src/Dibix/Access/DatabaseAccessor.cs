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

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters) => Invoke(commandText, commandType, parameters, Execute);

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout) => Invoke(commandText, commandType, parameters, commandTimeout, Execute);

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, ExecuteAsync);

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, ExecuteAsync);

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Invoke(commandText, commandType, parameters, QueryManyAndPostProcess<T>);

        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, QueryManyAsyncAndPostProcess<T>);

        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, buffered, cancellationToken, QueryManyAsyncAndPostProcess<T>);

        public IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, QueryManyAutoMultiMap<TReturn>);

        public Task<IEnumerable<TReturn>> QueryManyAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, cancellationToken, QueryManyAutoMultiMapAsync<TReturn>);

        T IDatabaseAccessor.QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Invoke(commandText, commandType, parameters, QuerySingle<T>);

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, QuerySingleAsync<T>);

        public TReturn QuerySingle<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, QuerySingleCore<TReturn>);

        public Task<TReturn> QuerySingleAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, cancellationToken, QuerySingleAsyncCore<TReturn>);

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Invoke(commandText, commandType, parameters, QuerySingleOrDefault<T>);

        Task<T> IDatabaseAccessor.QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, QuerySingleOrDefaultAsync<T>);

        public TReturn QuerySingleOrDefault<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, QuerySingleOrDefaultCore<TReturn>);

        public Task<TReturn> QuerySingleOrDefaultAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => Invoke(commandText, commandType, parameters, types, splitOn, cancellationToken, QuerySingleOrDefaultAsyncCore<TReturn>);

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => Invoke(commandText, commandType, parameters, QueryMultiple);

        Task<IMultipleResultReader> IDatabaseAccessor.QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Invoke(commandText, commandType, parameters, cancellationToken, QueryMultipleAsync);

        public FileEntity QueryFile(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            using DbCommand command = Connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;

            using DbCommandParameterCollector parametersCollector = new DbCommandParameterCollector(command, DbProviderAdapter);
            parameters.VisitInputParameters(parametersCollector.VisitInputParameter);

            DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
            FileEntity file = ReadFiles(reader).Single(commandText, commandType, parameters, defaultIfEmpty: false, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
            return file;
        }

        public async Task<FileEntity> QueryFileAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
#if NET
            await
#endif
            using DbCommand command = Connection.CreateCommand();
            command.CommandText = commandText;
            DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            FileEntity file = ReadFiles(reader).Single(commandText, commandType, parameters, defaultIfEmpty: false, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
            return file;
        }
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

        protected abstract IEnumerable<TReturn> Parse<TReturn>(IDataReader reader);
        #endregion

        #region Protected Methods
        protected virtual void DisposeConnection() => Connection?.Dispose();

        protected virtual void OnInfoMessage(string message) { }
        #endregion

        #region Private Methods
        private int Execute(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, commandTimeout: null);

        private Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => ExecuteAsync(commandText, commandType, parameters, commandTimeout: null, cancellationToken);

        private Task<IEnumerable<T>> QueryManyAsyncAndPostProcess<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => QueryManyAsyncAndPostProcess<T>(commandText, commandType, parameters, buffered: true, cancellationToken);
        private Task<IEnumerable<T>> QueryManyAsyncAndPostProcess<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken) => QueryManyAsync<T>(commandText, commandType, parameters, buffered, cancellationToken).PostProcess();

        private IEnumerable<T> QueryManyAndPostProcess<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => QueryMany<T>(commandText, commandType, parameters).PostProcess();

        private IEnumerable<TReturn> QueryManyAutoMultiMap<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => QueryManyAutoMultiMap<TReturn>(commandText, commandType, parameters, types, splitOn, buffered: true);
        private IEnumerable<TReturn> QueryManyAutoMultiMap<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool buffered) where TReturn : new()
        {
            ValidateMultiMapSegments(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            MultiMapperExtension multiMapperExtension = new MultiMapperExtension(multiMapper);
            Func<object[], TReturn> map = useProjection ? multiMapperExtension.MapRowWithProjection<TReturn> : multiMapperExtension.MapRowWithoutProjection<TReturn>;
            return QueryMany(commandText, commandType, parameters, types, map, splitOn, buffered).PostProcess(multiMapper);
        }
        private Task<IEnumerable<TReturn>> QueryManyAutoMultiMapAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where TReturn : new() => QueryManyAutoMultiMapAsync<TReturn>(commandText, commandType, parameters, types, splitOn, cancellationToken, buffered: true);
        private Task<IEnumerable<TReturn>> QueryManyAutoMultiMapAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken, bool buffered) where TReturn : new()
        {
            ValidateMultiMapSegments(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            MultiMapperExtension multiMapperExtension = new MultiMapperExtension(multiMapper);
            Func<object[], TReturn> map = useProjection ? multiMapperExtension.MapRowWithProjection<TReturn> : multiMapperExtension.MapRowWithoutProjection<TReturn>;
            return QueryManyAsync(commandText, commandType, parameters, types, map, splitOn, buffered, cancellationToken).PostProcess(multiMapper);
        }

        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: false);
        private T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: true);
        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty)
        {
            IEnumerable<T> result = QueryMany<T>(commandText, commandType, parameters, buffered: false).PostProcess();
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }

        private T QuerySingleCore<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where T : new() => QuerySingle<T>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: false);
        private T QuerySingleOrDefaultCore<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where T : new() => QuerySingle<T>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: true);
        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = QueryManyAutoMultiMap<T>(commandText, commandType, parameters, types, splitOn, buffered: false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }

        private async Task<T> QuerySingleAsyncCore<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where T : new() => await QuerySingleAsync<T>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: false, cancellationToken).ConfigureAwait(false);
        private async Task<T> QuerySingleOrDefaultAsyncCore<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken) where T : new() => await QuerySingleAsync<T>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: true, cancellationToken).ConfigureAwait(false);
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool defaultIfEmpty, CancellationToken cancellationToken) where T : new()
        {
            IEnumerable<T> result = await QueryManyAutoMultiMapAsync<T>(commandText, commandType, parameters, types, splitOn, cancellationToken, buffered: false).ConfigureAwait(false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => await QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: false, cancellationToken).ConfigureAwait(false);
        private async Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => await QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: true, cancellationToken).ConfigureAwait(false);
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty, CancellationToken cancellationToken)
        {
            IEnumerable<T> result = await QueryManyAsync<T>(commandText, commandType, parameters, buffered: false, cancellationToken).PostProcess().ConfigureAwait(false);
            return result.Single(commandText, commandType, parameters, defaultIfEmpty, collectTSqlDebugStatement: DbProviderAdapter.UsesTSql);
        }

        private IEnumerable<FileEntity> ReadFiles(DbDataReader reader)
        {
            int dataIndex = reader.GetOrdinal(nameof(FileEntity.Data));
            foreach (FileEntity entity in Parse<FileEntity>(reader))
            {
                entity.Data = new ReaderOwningStream(reader.GetStream(dataIndex), reader);
                yield return entity;
                break;
            }
        }

        private T Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<string, CommandType, ParametersVisitor, T> handler)
        {
            try { return handler(commandText, commandType, parameters); }
            catch (DatabaseAccessException) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private T Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, Func<string, CommandType, ParametersVisitor, int?, T> handler)
        {
            try { return handler(commandText, commandType, parameters, commandTimeout); }
            catch (DatabaseAccessException) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private T Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, Func<string, CommandType, ParametersVisitor, Type[], string, T> handler)
        {
            try { return handler(commandText, commandType, parameters, types, splitOn); }
            catch (DatabaseAccessException) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private async Task<T> Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken, Func<string, CommandType, ParametersVisitor, CancellationToken, Task<T>> action)
        {
            try { return await action(commandText, commandType, parameters, cancellationToken).ConfigureAwait(false); }
            catch (DatabaseAccessException) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception, DbProviderAdapter.TryGetSqlErrorNumber(exception.InnerException), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private async Task<T> Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken, Func<string, CommandType, ParametersVisitor, bool, CancellationToken, Task<T>> action)
        {
            try { return await action(commandText, commandType, parameters, buffered, cancellationToken).ConfigureAwait(false); }
            catch (DatabaseAccessException) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception, DbProviderAdapter.TryGetSqlErrorNumber(exception.InnerException), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, DbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: DbProviderAdapter.UsesTSql); }
        }
        private async Task<T> Invoke<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, CancellationToken cancellationToken, Func<string, CommandType, ParametersVisitor, Type[], string, CancellationToken, Task<T>> action)
        {
            try { return await action(commandText, commandType, parameters, types, splitOn, cancellationToken).ConfigureAwait(false); }
            catch (DatabaseAccessException) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException) { throw databaseAccessException; }
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