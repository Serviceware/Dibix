using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        T IMultipleResultReader.ReadSingle<T>() => Execute(() => ReadSingle<T>().PostProcess());

        Task<T> IMultipleResultReader.ReadSingleAsync<T>() => Execute(() => ReadSingleAsync<T>().PostProcess());

        public TReturn ReadSingle<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadManyCore<TReturn>(types, splitOn).Single());

        T IMultipleResultReader.ReadSingleOrDefault<T>() => Execute(() => ReadSingleOrDefault<T>().PostProcess());

        Task<T> IMultipleResultReader.ReadSingleOrDefaultAsync<T>() => Execute(() => ReadSingleOrDefaultAsync<T>().PostProcess());

        public TReturn ReadSingleOrDefault<TReturn>(Type[] types, string splitOn) where TReturn : new() => Execute(() => ReadManyCore<TReturn>(types, splitOn).SingleOrDefault());
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<T> ReadMany<T>();
        
        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>();

        protected abstract IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn);

        protected abstract T ReadSingle<T>();

        protected abstract Task<T> ReadSingleAsync<T>();

        protected abstract T ReadSingleOrDefault<T>();

        protected abstract Task<T> ReadSingleOrDefaultAsync<T>();
        #endregion

        #region Private Methods
        private IEnumerable<TReturn> ReadManyCore<TReturn>(Type[] types, string splitOn) where TReturn : new()
        {
            ValidateParameters(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return ReadMany(types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn).PostProcess(multiMapper);
        }

        private T Execute<T>(Func<T> action)
        {
            try { return action(); }
            catch (Exception ex) { throw DatabaseAccessException.Create(_commandType, _commandText, _parameters, ex, _isSqlClient); }
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