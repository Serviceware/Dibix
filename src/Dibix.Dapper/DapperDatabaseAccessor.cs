using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;

namespace Dibix.Dapper
{
    public sealed class DapperDatabaseAccessor : DatabaseAccessor, IDatabaseAccessor, IDisposable
    {
        #region Fields
        private readonly IDbTransaction _transaction;
        #endregion

        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection) : this(connection, null, null) { }
        public DapperDatabaseAccessor(DbConnection connection, Action onDispose) : this(connection, null, onDispose) { }
        public DapperDatabaseAccessor(DbConnection connection, IDbTransaction transaction, Action onDispose) : base(connection, onDispose)
        {
            this._transaction = transaction;
        }
        #endregion

        #region Overrides
        protected override int Execute(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            return base.Connection.Execute(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override Task<int> ExecuteAsync(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            return base.Connection.ExecuteAsync(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override IEnumerable<T> QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.Query<T>(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond>();
            return base.Connection.Query(sql, map, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird>();
            return base.Connection.Query(sql, map, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth>();
            return base.Connection.Query(sql, map, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth>();
            return base.Connection.Query(sql, map, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>();
            return base.Connection.Query(sql, map, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8]);
            return base.Connection.Query(sql, types, mapWrapper, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8], (TTenth)x[9], (TEleventh)x[10]);
            return base.Connection.Query(sql, types, mapWrapper, parameters.AsDapperParams(), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override T QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingle<T>(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override Task<T> QuerySingleAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingleAsync<T>(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override T QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingleOrDefault<T>(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
        }

        protected override IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            SqlMapper.GridReader reader = base.Connection.QueryMultiple(sql, parameters.AsDapperParams(), this._transaction, commandType: commandType);
            return new DapperGridResultReader(reader);
        }
        #endregion
    }
}