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

        public IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadManyCore<TReturn>(types, splitOn));

        T IMultipleResultReader.ReadSingle<T>() => Execute(() => ReadSingle<T>(defaultIfEmpty: false).PostProcess());

        Task<T> IMultipleResultReader.ReadSingleAsync<T>() => Execute(() => ReadSingleAsync<T>(defaultIfEmpty: false).PostProcess());

        public TReturn ReadSingle<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadSingle<TReturn>(types, splitOn, defaultIfEmpty: false));

        T IMultipleResultReader.ReadSingleOrDefault<T>() => Execute(() => ReadSingle<T>(defaultIfEmpty: true).PostProcess());

        Task<T> IMultipleResultReader.ReadSingleOrDefaultAsync<T>() => Execute(() => ReadSingleAsync<T>(defaultIfEmpty: true).PostProcess());

        public TReturn ReadSingleOrDefault<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadSingle<TReturn>(types, splitOn, defaultIfEmpty: true));
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<T> ReadMany<T>();
        protected abstract IEnumerable<T> ReadMany<T>(bool buffered);
        
        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>();
        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>(bool buffered);

        protected abstract IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered);

        //protected abstract T ReadSingle<T>();

        //protected abstract Task<T> ReadSingleAsync<T>();

        //protected abstract T ReadSingleOrDefault<T>();

        //protected abstract Task<T> ReadSingleOrDefaultAsync<T>();
        #endregion

        #region Private Methods
        private IEnumerable<TReturn> ReadManyCore<TReturn>(Type[] types, string splitOn, bool buffered = true) where TReturn : new()
        {
            ValidateParameters(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return ReadMany(types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered).PostProcess(multiMapper);
        }

        private T ReadSingle<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = ReadMany<T>(buffered: false);
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, _isSqlClient);
        }
        private T ReadSingle<T>(Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = ReadManyCore<T>(types, splitOn, buffered: false);
            return result.Single(_commandText, _commandType, _parameters, defaultIfEmpty, _isSqlClient);
        }
        private async Task<T> ReadSingleAsync<T>(bool defaultIfEmpty)
        {
            IEnumerable<T> result = await ReadManyAsync<T>(buffered: false).ConfigureAwait(false);
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