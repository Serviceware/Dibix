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

        public abstract int Execute(string sql, CommandType commandType, IParametersVisitor parameters);

        public abstract Task<int> ExecuteAsync(string sql, CommandType commandType, IParametersVisitor parameters);

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters) => this.QueryMany<T>(sql, commandType, parameters).PostProcess();

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(sql, commandType, parameters, (a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper);
        }

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn) => this.QueryMany(sql, commandType, parameters, map, splitOn).PostProcess();

        T IDatabaseAccessor.QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters) => this.QuerySingle<T>(sql, commandType, parameters).PostProcess();

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters) => this.QuerySingleAsync<T>(sql, commandType, parameters).PostProcess();

        // SubProcessOverview
        public TReturn QuerySingle<TReturn, TSecond>(string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(sql, CommandType.Text, parameters, (a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        }

        public TReturn QuerySingle<TReturn, TSecond, TThird>(string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TReturn>(sql, CommandType.Text, parameters, (a, b, c) => multiMapper.MapRow<TReturn>(false, a, b, c), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        }

        // OrderManagement (GetCategoryDetail)
        public TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(sql, CommandType.Text, parameters, (a, b, c, d) => multiMapper.MapRow<TReturn>(false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        }

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters) => this.QuerySingleOrDefault<T>(sql, commandType, parameters).PostProcess();

        public abstract IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters);
        #endregion

        #region Abstract Methods
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
        #endregion

        #region Private Methods
        private void DisposeConnection() => this.Connection?.Dispose();
        #endregion

        #region IDisposable Members
        void IDisposable.Dispose() => this._onDispose?.Invoke();
        #endregion
    }
}