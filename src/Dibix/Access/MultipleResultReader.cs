using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class MultipleResultReader : IMultipleResultReader, IDisposable
    {
        #region Fields
        private readonly string _commandText;
        private readonly CommandType _commandType;
        private readonly ParametersVisitor _parameters;
        private readonly DbProviderAdapter _dbProviderAdapter;
        #endregion

        #region Constructor
        protected MultipleResultReader(string commandText, CommandType commandType, ParametersVisitor parameters, DbProviderAdapter dbProviderAdapter)
        {
            _commandText = commandText;
            _commandType = commandType;
            _parameters = parameters;
            _dbProviderAdapter = dbProviderAdapter;
        }
        #endregion

        #region IMultipleResultReader Members
        public abstract bool IsConsumed { get; }

        IEnumerable<T> IMultipleResultReader.ReadMany<T>() => Invoke(ReadManyAndPostProcess<T>);

        Task<IEnumerable<T>> IMultipleResultReader.ReadManyAsync<T>() => Invoke<Task<IEnumerable<T>>>(ReadManyAndPostProcessAsync<T>);

        public IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, string splitOn) where TReturn : new() => Invoke(types, splitOn, ReadManyAutoMultiMap<TReturn>);

        // NOTE: Apparently there is no async overload in Dapper using multimap
        //public Task<IEnumerable<TReturn>> ReadManyAsync<TReturn>(Type[] types, string splitOn) where TReturn : new() => Invoke(types, splitOn, ReadManyAutoMultiMapAsync<TReturn>);

        T IMultipleResultReader.ReadSingle<T>() => Invoke(ReadSingle<T>);

        Task<T> IMultipleResultReader.ReadSingleAsync<T>() => Invoke(ReadSingleAsync<T>);

        public TReturn ReadSingle<TReturn>(Type[] types, string splitOn) where TReturn : new() => Invoke(types, splitOn, ReadSingleCore<TReturn>);

        T IMultipleResultReader.ReadSingleOrDefault<T>() => Invoke(ReadSingleOrDefault<T>);

        Task<T> IMultipleResultReader.ReadSingleOrDefaultAsync<T>() => Invoke(ReadSingleOrDefaultAsync<T>);

        public TReturn ReadSingleOrDefault<TReturn>(Type[] types, string splitOn) where TReturn : new() => Invoke(types, splitOn, ReadSingleOrDefaultCore<TReturn>);
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<T> ReadMany<T>();
        protected abstract IEnumerable<T> ReadMany<T>(bool buffered);

        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>();
        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>(bool buffered);

        protected abstract IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered);

        // NOTE: Apparently there is no async overload in Dapper using multimap
        //protected abstract Task<IEnumerable<TReturn>> ReadManyAsync<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered);
        #endregion

        #region Private Methods
        private IEnumerable<T> ReadManyAndPostProcess<T>() => ReadMany<T>().PostProcess();

        private Task<IEnumerable<T>> ReadManyAndPostProcessAsync<T>() => ReadManyAsync<T>().PostProcess();

        private IEnumerable<TReturn> ReadManyAutoMultiMap<TReturn>(Type[] types, string splitOn) where TReturn : new() => ReadManyAutoMultiMap<TReturn>(types, splitOn, buffered: true);
        private IEnumerable<TReturn> ReadManyAutoMultiMap<TReturn>(Type[] types, string splitOn, bool buffered) where TReturn : new()
        {
            ValidateParameters(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return ReadMany(types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered).PostProcess(multiMapper);
        }
        // NOTE: Apparently there is no async overload in Dapper using multimap
        //private Task<IEnumerable<TReturn>> ReadManyAutoMultiMapAsync<TReturn>(Type[] types, string splitOn, bool buffered = true) where TReturn : new()
        //{
        //    ValidateParameters(types, splitOn);
        //    MultiMapper multiMapper = new MultiMapper();
        //    bool useProjection = types[0] != typeof(TReturn);
        //    return ReadManyAsync(types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered).PostProcess(multiMapper);
        //}

        private T ReadSingle<T>() => ReadSingleCore<T>(defaultIfEmpty: false);
        private T ReadSingleOrDefault<T>() => ReadSingleCore<T>(defaultIfEmpty: true);
        private T ReadSingleCore<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = ReadMany<T>(buffered: false).PostProcess();
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql);
        }

        private T ReadSingleCore<T>(Type[] types, string splitOn) where T : new() => ReadSingleCore<T>(types, splitOn, defaultIfEmpty: false);
        private T ReadSingleOrDefaultCore<T>(Type[] types, string splitOn) where T : new() => ReadSingleCore<T>(types, splitOn, defaultIfEmpty: true);
        private T ReadSingleCore<T>(Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = ReadManyAutoMultiMap<T>(types, splitOn, buffered: false).PostProcess();
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql);
        }

        private Task<T> ReadSingleAsync<T>() => ReadSingleAsync<T>(defaultIfEmpty: false);
        private Task<T> ReadSingleOrDefaultAsync<T>() => ReadSingleAsync<T>(defaultIfEmpty: true);
        private async Task<T> ReadSingleAsync<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = await ReadManyAsync<T>(buffered: false).PostProcess().ConfigureAwait(false);
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql);
        }

        private T Invoke<T>(Func<T> action)
        {
            try { return action(); }
            catch (DatabaseAccessException) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception, _dbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql); }
        }
        private T Invoke<T>(Type[] types, string splitOn, Func<Type[], string, T> action)
        {
            try { return action(types, splitOn); }
            catch (DatabaseAccessException) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception, _dbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql); }
        }
        private async Task<T> Invoke<T>(Func<Task<T>> action)
        {
            try { return await action().ConfigureAwait(false); }
            catch (DatabaseAccessException) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception.InnerException ?? exception, _dbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql); }
            catch (Exception exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception, _dbProviderAdapter.TryGetSqlErrorNumber(exception), collectTSqlDebugStatement: _dbProviderAdapter.UsesTSql); }
        }

        private static void ValidateParameters(IReadOnlyCollection<Type> types, string splitOn)
        {
            MultiMapUtility.ValidateParameters(types, splitOn);
        }
        #endregion

        #region IDisposable Members
        public abstract void Dispose();
        #endregion
    }
}