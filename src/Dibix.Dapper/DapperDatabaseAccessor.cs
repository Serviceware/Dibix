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
        private readonly IDbTransaction _defaultTransaction;
        private readonly int? _defaultCommandTimeout;
        private readonly Action _onDispose;
        #endregion

        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection = null, IDbTransaction defaultTransaction = null, int? defaultCommandTimeout = null, Action onDispose = null) : base(connection)
        {
            _defaultTransaction = defaultTransaction;
            _defaultCommandTimeout = defaultCommandTimeout;
            _onDispose = onDispose;
            ConfigureDapper();
        }
        #endregion

        #region Overrides
        protected override int Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters)
        {
            return base.Connection.Execute(commandText, CollectParameters(parameters), _defaultTransaction, commandTimeout ?? _defaultCommandTimeout, commandType);
        }

        protected override Task<int> ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), _defaultTransaction, commandTimeout ?? _defaultCommandTimeout, commandType, cancellationToken: cancellationToken);
            return base.Connection.ExecuteAsync(command);
        }

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.Query<T>(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout);
        }

        protected override Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandFlags flags = buffered ? CommandFlags.Buffered : CommandFlags.None;
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, flags: flags, cancellationToken: cancellationToken);
            return base.Connection.QueryAsync<T>(command);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>();
            return base.Connection.Query(commandText, map, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8]);
            return base.Connection.Query(commandText, types, mapWrapper, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>();
            Type[] types = { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh) };
            Func<object[], TReturn> mapWrapper = x => map((TFirst)x[0], (TSecond)x[1], (TThird)x[2], (TFourth)x[3], (TFifth)x[4], (TSixth)x[5], (TSeventh)x[6], (TEighth)x[7], (TNinth)x[8], (TTenth)x[9], (TEleventh)x[10]);
            return base.Connection.Query(commandText, types, mapWrapper, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, splitOn: splitOn);
        }

        protected override T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingle<T>(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout);
        }

        protected override Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, cancellationToken: cancellationToken);
            return base.Connection.QuerySingleAsync<T>(command);
        }

        protected override T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.QuerySingleOrDefault<T>(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout);
        }

        protected override Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout, cancellationToken: cancellationToken);
            return base.Connection.QuerySingleOrDefaultAsync<T>(command);
        }

        protected override IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            SqlMapper.GridReader reader = base.Connection.QueryMultiple(commandText, CollectParameters(parameters), _defaultTransaction, commandType: commandType, commandTimeout: _defaultCommandTimeout);
            return new DapperGridResultReader(reader, commandText, commandType, parameters);
        }

        protected override async Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            SqlMapper.GridReader reader = await base.Connection.QueryMultipleAsync(new CommandDefinition(commandText, CollectParameters(parameters), _defaultTransaction, commandTimeout: _defaultCommandTimeout, commandType, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return new DapperGridResultReader(reader, commandText, commandType, parameters);
        }

        protected override void DisposeConnection()
        {
            if (_onDispose != null)
                _onDispose.Invoke();
            else
                base.DisposeConnection();
        }
        #endregion

        #region Protected Methods
        protected static object CollectParameters(ParametersVisitor parametersVisitor)
        {
            Guard.IsNotNull(parametersVisitor, nameof(parametersVisitor));
            DynamicParameters @params = new DynamicParameters();
            parametersVisitor.VisitInputParameters((name, dataType, value, isOutput, customInputType) => @params.Add(name: name, value: NormalizeParameterValue(value), dbType: NormalizeParameterDbType(dataType, customInputType), direction: isOutput ? ParameterDirection.Output : (ParameterDirection?)null));
            return new DynamicParametersWrapper(@params, parametersVisitor);
        }
        #endregion

        #region Private Methods
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

            object SqlMapper.IParameterLookup.this[string name] => _parameterLookup[name];

            public DynamicParametersWrapper(DynamicParameters impl, ParametersVisitor parametersVisitor)
            {
                _impl = impl;
                _dynamicParameters = impl;
                _parameterLookup = impl;
                _parameterCallbacks = impl;
                _parametersVisitor = parametersVisitor;
            }

            void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity) => _dynamicParameters.AddParameters(command, identity);
            
            void SqlMapper.IParameterCallbacks.OnCompleted()
            {
                _parametersVisitor.VisitOutputParameters(name => _impl.Get<object>(name));
                _parameterCallbacks.OnCompleted();
            }
        }
        #endregion
    }
}