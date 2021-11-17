using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Dibix.Dapper
{
    public class DapperDatabaseAccessor : DatabaseAccessor, IDatabaseAccessor, IDisposable
    {
        #region Fields
        private readonly IDbTransaction _transaction;
        #endregion

        #region Constructor
        static DapperDatabaseAccessor() => ConfigureDapper();
        public DapperDatabaseAccessor(DbConnection connection) : this(connection, null, null) { }
        public DapperDatabaseAccessor(DbConnection connection, Action onDispose) : this(connection, null, onDispose) { }
        public DapperDatabaseAccessor(DbConnection connection, IDbTransaction transaction, Action onDispose) : base(connection, onDispose)
        {
            this._transaction = transaction;
        }
        #endregion

        #region Overrides
        protected override int Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters)
        {
            return base.Connection.Execute(commandText, PrepareParameters(parameters), this._transaction, commandTimeout, commandType);
        }

        protected override Task<int> ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            CommandDefinition command = new CommandDefinition(commandText, PrepareParameters(parameters), this._transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return base.Connection.ExecuteAsync(command);
        }

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.Query<T>(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType);
        }

        protected override Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandFlags flags = buffered ? CommandFlags.Buffered : CommandFlags.None;
            CommandDefinition command = new CommandDefinition(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType, flags: flags, cancellationToken: cancellationToken);
            return base.Connection.QueryAsync<T>(command);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond>();
            return base.Connection.Query(commandText, map, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird>();
            return base.Connection.Query(commandText, map, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth>();
            return base.Connection.Query(commandText, map, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth>();
            return base.Connection.Query(commandText, map, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>();
            return base.Connection.Query(commandText, map, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8]);
            return base.Connection.Query(commandText, types, mapWrapper, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8], (TTenth)x[9], (TEleventh)x[10]);
            return base.Connection.Query(commandText, types, mapWrapper, PrepareParameters(parameters), this._transaction, commandType: commandType, splitOn: splitOn);
        }

        protected override T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingle<T>(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType);
        }

        protected override Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandDefinition command = new CommandDefinition(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType, cancellationToken: cancellationToken);
            return base.Connection.QuerySingleAsync<T>(command);
        }

        protected override T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingleOrDefault<T>(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType);
        }

        protected override IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            SqlMapper.GridReader reader = base.Connection.QueryMultiple(commandText, PrepareParameters(parameters), this._transaction, commandType: commandType);
            return new DapperGridResultReader(reader, commandText, commandType, parameters);
        }

        protected override async Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            SqlMapper.GridReader reader = await base.Connection.QueryMultipleAsync(new CommandDefinition(commandText, PrepareParameters(parameters), this._transaction, commandTimeout: null, commandType, cancellationToken: cancellationToken));
            return new DapperGridResultReader(reader, commandText, commandType, parameters);
        }
        #endregion

        #region Private Methods
        private static object PrepareParameters(ParametersVisitor parametersVisitor)
        {
            Guard.IsNotNull(parametersVisitor, nameof(parametersVisitor));
            DynamicParameters @params = new DynamicParameters();
            parametersVisitor.VisitInputParameters((name, dataType, value, isOutput, customInputType) => @params.Add(name: name, value: NormalizeParameterValue(value), dbType: NormalizeParameterDbType(dataType, customInputType), direction: isOutput ? ParameterDirection.Output : (ParameterDirection?)null));
            return new DynamicParametersWrapper(@params, parametersVisitor);
        }

        private static object NormalizeParameterValue(object value)
        {
            if (value is StructuredType tvp)
                return new DapperStructuredTypeParameter(tvp.TypeName, tvp.GetRecords);

            return value;
        }

        private static DbType? NormalizeParameterDbType(DbType dbType, CustomInputType customInputType)
        {
            if (dbType == DbType.Xml)
                return null; // You would guess DbType.Xml, but since Dapper treats .NET XML types (i.E. XElement) as custom types, DbType = null is expected

            if (customInputType != default)
                return null; // Same weird logic like above. Dapper will only resolve the custom type handler, if the db type is null.

            return dbType;
        }

        private static void ConfigureDapper()
        {
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());
        }
        #endregion

        #region Nested Types
        private sealed class DynamicParametersWrapper : SqlMapper.IDynamicParameters, SqlMapper.IParameterLookup, SqlMapper.IParameterCallbacks
        {
            private readonly DynamicParameters _impl;
            private readonly SqlMapper.IDynamicParameters _dynamicParameters;
            private readonly SqlMapper.IParameterLookup _parameterLookup;
            private readonly SqlMapper.IParameterCallbacks _parameterCallbacks;
            private readonly ParametersVisitor _parametersVisitor;

            object SqlMapper.IParameterLookup.this[string name] => this._parameterLookup[name];

            public DynamicParametersWrapper(DynamicParameters impl, ParametersVisitor parametersVisitor)
            {
                this._impl = impl;
                this._dynamicParameters = impl;
                this._parameterLookup = impl;
                this._parameterCallbacks = impl;
                this._parametersVisitor = parametersVisitor;
            }

            void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity) => this._dynamicParameters.AddParameters(command, identity);
            
            void SqlMapper.IParameterCallbacks.OnCompleted()
            {
                this._parametersVisitor.VisitOutputParameters(name => this._impl.Get<object>(name));
                this._parameterCallbacks.OnCompleted();
            }
        }
        #endregion
    }
}