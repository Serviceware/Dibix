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
        private readonly bool _isSqlClient;
        private readonly ParametersVisitor _parameters;
        #endregion

        #region Constructor
        protected MultipleResultReader(string commandText, CommandType commandType, ParametersVisitor parameters, bool isSqlClient)
        {
            _commandText = commandText;
            _commandType = commandType;
            _parameters = parameters;
            _isSqlClient = isSqlClient;
        }
        #endregion

        #region IMultipleResultReader Members
        public abstract bool IsConsumed { get; }

        IEnumerable<T> IMultipleResultReader.ReadMany<T>() => Execute(() => ReadMany<T>().PostProcess());

        Task<IEnumerable<T>> IMultipleResultReader.ReadManyAsync<T>() => Execute(() => ReadManyAsync<T>().PostProcess());

        public IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadManyAutoMultiMap<TReturn>(types, splitOn));

        // NOTE: Apparently there is no async overload in Dapper using multimap
        //public Task<IEnumerable<TReturn>> ReadManyAsync<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadManyAutoMultiMapAsync<TReturn>(types, splitOn));

        T IMultipleResultReader.ReadSingle<T>() => Execute(() => ReadSingle<T>(defaultIfEmpty: false));

        Task<T> IMultipleResultReader.ReadSingleAsync<T>() => Execute(() => ReadSingleAsync<T>(defaultIfEmpty: false));

        public TReturn ReadSingle<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadSingle<TReturn>(types, splitOn, defaultIfEmpty: false));

        T IMultipleResultReader.ReadSingleOrDefault<T>() => Execute(() => ReadSingle<T>(defaultIfEmpty: true));

        Task<T> IMultipleResultReader.ReadSingleOrDefaultAsync<T>() => Execute(() => ReadSingleAsync<T>(defaultIfEmpty: true));

        public TReturn ReadSingleOrDefault<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadSingle<TReturn>(types, splitOn, defaultIfEmpty: true));
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
        private IEnumerable<TReturn> ReadManyAutoMultiMap<TReturn>(Type[] types, string splitOn, bool buffered = true) where TReturn : new()
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

        private T ReadSingle<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = ReadMany<T>(buffered: false).PostProcess();
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, _isSqlClient);
        }
        private T ReadSingle<T>(Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = ReadManyAutoMultiMap<T>(types, splitOn, buffered: false).PostProcess();
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, _isSqlClient);
        }
        private async Task<T> ReadSingleAsync<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = await ReadManyAsync<T>(buffered: false).PostProcess().ConfigureAwait(false);
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, _isSqlClient);
        }

        private T Execute<T>(Func<T> action)
        {
            try { return action(); }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException && databaseAccessException.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception.InnerException ?? exception, _isSqlClient); }
            catch (Exception exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception, _isSqlClient); }
        }
        private async Task<T> Execute<T>(Func<Task<T>> action)
        {
            try { return await action().ConfigureAwait(false); }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException && databaseAccessException.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception.InnerException ?? exception, _isSqlClient); }
            catch (Exception exception) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, exception, _isSqlClient); }
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