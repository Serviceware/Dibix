using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Dapper;

namespace Dibix.Dapper
{
    public sealed class DapperDatabaseAccessor : IDatabaseAccessor, IDisposable
    {
        #region Fields
        private readonly DbConnection _connection;
        private readonly DapperMappingCheck _mappingCheck;
        private readonly Action _onDispose;
        #endregion

        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection) : this(connection, DapperMappingBehavior.Strict, null) { }
        public DapperDatabaseAccessor(DbConnection connection, Action onDispose) : this(connection, DapperMappingBehavior.Strict, onDispose) { }
        private DapperDatabaseAccessor(DbConnection connection, DapperMappingBehavior mappingBehavior, Action onDispose)
        {
            this._connection = connection;
            this._mappingCheck = DetermineMappingCheck(mappingBehavior);
            this._onDispose = onDispose ?? this.DisposeConnection;
        }
        #endregion

        #region IDatabaseAccessor Members
        public IParameterBuilder Parameters()
        {
            return new ParameterBuilder();
        }

        public int Execute(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            return this._connection.Execute(sql, parameters.AsDapperParams(), commandType: commandType);
        }

        public IEnumerable<T> QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            this._mappingCheck.Check<T>();
            return this._connection.Query<T>(sql, parameters.AsDapperParams(), commandType: commandType);
        }

        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond>();
            return this._connection.Query(sql, map, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }
        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird>();
            return this._connection.Query(sql, map, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }
        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird, TFourth>();
            return this._connection.Query(sql, map, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }
        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird, TFourth, TFifth>();
            return this._connection.Query(sql, map, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }
        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8]);
            return this._connection.Query(sql, types, mapWrapper, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }
        public IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8], (TTenth)x[9], (TEleventh)x[10]);
            return this._connection.Query(sql, types, mapWrapper, parameters.AsDapperParams(), commandType: commandType, splitOn: splitOn);
        }

        public T QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            this._mappingCheck.Check<T>();
            return this._connection.QuerySingle<T>(sql, parameters.AsDapperParams(), commandType: commandType);
        }

        public T QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            this._mappingCheck.Check<T>();
            return this._connection.QuerySingleOrDefault<T>(sql, parameters.AsDapperParams(), commandType: commandType);
        }

        public IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            SqlMapper.GridReader reader = this._connection.QueryMultiple(sql, parameters.AsDapperParams(), commandType: commandType);
            return new DapperGridResultReader(reader, this._mappingCheck);
        }
        #endregion

        #region Private Methods
        private static DapperMappingCheck DetermineMappingCheck(DapperMappingBehavior mappingBehavior)
        {
            switch (mappingBehavior)
            {
                case DapperMappingBehavior.Pragmatic: return new DapperMappingCheckPragmatic();
                case DapperMappingBehavior.Strict: return new DapperMappingCheckStrict();
                default: throw new ArgumentOutOfRangeException(nameof(mappingBehavior), mappingBehavior, null);
            }
        }

        private void DisposeConnection() => this._connection?.Dispose();
        #endregion

        #region IDisposable Members
        void IDisposable.Dispose() => this._onDispose?.Invoke();
        #endregion
    }
}