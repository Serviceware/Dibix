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
        #endregion

        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection, DapperMappingBehavior mappingBehavior)
        {
            this._connection = connection;
            this._mappingCheck = DetermineMappingCheck(mappingBehavior);
        }
        #endregion

        #region IDatabaseQuery Members
        public IParameterBuilder Parameters()
        {
            return new ParameterBuilder();
        }

        public int Execute(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            return this._connection.Execute(sql, parameters.AsDapperParams(), commandType: commandType);
        }

        public T ExecuteScalar<T>(string sql, CommandType commandType, IParametersVisitor parameters)
        {
            this._mappingCheck.Check<T>();
            return this._connection.ExecuteScalar<T>(sql, parameters.AsDapperParams(), commandType: commandType);
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
                default: throw new ArgumentOutOfRangeException("mappingBehavior", mappingBehavior, null);
            }
        }
        #endregion

        #region IDisposable Members
        void IDisposable.Dispose()
        {
            if (this._connection != null)
                this._connection.Dispose();
        }
        #endregion
    }
}