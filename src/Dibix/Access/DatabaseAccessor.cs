using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class DatabaseAccessor : IDatabaseAccessor, IDisposable
    {
        #region Fields
        private readonly Action _onDispose;
        #endregion

        #region Properties
        protected DbConnection Connection { get; }
        #endregion

        #region Constructor
        protected DatabaseAccessor(DbConnection connection) : this(connection, null) { }
        protected DatabaseAccessor(DbConnection connection, Action onDispose)
        {
            this.Connection = connection;
            this._onDispose = onDispose ?? this.DisposeConnection;
        }
        #endregion

        #region IDatabaseAccessor Members
        public IParameterBuilder Parameters() => new ParameterBuilder();

        int IDatabaseAccessor.Execute(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.Execute(sql, commandType, parameters));

        Task<int> IDatabaseAccessor.ExecuteAsync(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.ExecuteAsync(sql, commandType, parameters));

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.QueryMany<T>(sql, commandType, parameters).PostProcess());

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(sql, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(sql, commandType, parameters, (a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper);
        });

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(sql, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TReturn>(sql, commandType, parameters, (a, b, c) => multiMapper.MapRow<TReturn>(false, a, b, c), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn) => Execute(sql, commandType, parameters, () => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess());

        T IDatabaseAccessor.QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.QuerySingle<T>(sql, commandType, parameters).PostProcess());

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.QuerySingleAsync<T>(sql, commandType, parameters).PostProcess());

        public TReturn QuerySingle<TReturn, TSecond>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(sql, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(sql, commandType, parameters, (a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(sql, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TReturn>(sql, commandType, parameters, (a, b, c) => multiMapper.MapRow<TReturn>(false, a, b, c), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(sql, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(sql, commandType, parameters, (a, b, c, d) => multiMapper.MapRow<TReturn>(false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.QuerySingleOrDefault<T>(sql, commandType, parameters).PostProcess());

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters) => Execute(sql, commandType, parameters, () => this.QueryMultiple(sql, commandType, parameters));
        #endregion

        #region Abstract Methods
        protected abstract int Execute(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract Task<int> ExecuteAsync(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract IEnumerable<T> QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn);

        protected abstract T QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract Task<T> QuerySingleAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract T QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters);

        protected abstract IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters);
        #endregion

        #region Private Methods
        private static T Execute<T>(string sql, CommandType commandType, IParametersVisitor parameters, Func<T> action)
        {
            try { return action(); }
            catch (Exception ex) { throw DatabaseAccessException.Create(commandType, sql, parameters, ex); }
        }

        private void DisposeConnection() => this.Connection?.Dispose();
        #endregion

        #region IDisposable Members
        void IDisposable.Dispose() => this._onDispose?.Invoke();
        #endregion
    }
}