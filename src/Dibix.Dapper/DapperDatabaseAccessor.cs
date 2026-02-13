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
        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection, DatabaseAccessorOptions options) : base(connection, options)
        {
            ConfigureDapper();
        }
        #endregion

        #region Overrides
        protected override int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout)
        {
            return base.Connection.Execute(commandText, CollectParameters(parameters), Options.DefaultTransaction, commandTimeout ?? Options.DefaultCommandTimeout, commandType);
        }

        protected override Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken)
        {
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), Options.DefaultTransaction, commandTimeout ?? Options.DefaultCommandTimeout, commandType, cancellationToken: cancellationToken);
            return base.Connection.ExecuteAsync(command);
        }

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.Query<T>(commandText, CollectParameters(parameters), Options.DefaultTransaction, commandTimeout: Options.DefaultCommandTimeout, commandType: commandType);
        }

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered)
        {
            DecoratedTypeMap.Adapt<T>();
            return base.Connection.Query<T>(commandText, CollectParameters(parameters), Options.DefaultTransaction, buffered, Options.DefaultCommandTimeout, commandType);
        }

        protected override Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandFlags flags = buffered ? CommandFlags.Buffered : CommandFlags.None;
            CommandDefinition command = new CommandDefinition(commandText, CollectParameters(parameters), Options.DefaultTransaction, Options.DefaultCommandTimeout, commandType, flags, cancellationToken);
            return base.Connection.QueryAsync<T>(command);
        }

        protected override IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered)
        {
            DecoratedTypeMap.Adapt(types);
            return base.Connection.Query(commandText, types, map, CollectParameters(parameters), Options.DefaultTransaction, splitOn: splitOn, commandTimeout: Options.DefaultCommandTimeout, commandType: commandType, buffered: buffered);
        }

        protected override Task<IEnumerable<TReturn>> QueryManyAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt(types);
            // NOTE: Apparently there is no overload in Dapper that either accepts CancellationToken or CommandDefinition and Type[]
            return Connection.QueryAsync(commandText, types, map, CollectParameters(parameters), Options.DefaultTransaction, splitOn: splitOn, commandTimeout: Options.DefaultCommandTimeout, commandType: commandType, buffered: buffered);
        }

        protected override IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            SqlMapper.GridReader reader = base.Connection.QueryMultiple(commandText, CollectParameters(parameters), Options.DefaultTransaction, commandTimeout: Options.DefaultCommandTimeout, commandType: commandType);
            return new DapperGridResultReader(reader, commandText, commandType, parameters, DbProviderAdapter, Options);
        }

        protected override async Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            SqlMapper.GridReader reader = await base.Connection.QueryMultipleAsync(new CommandDefinition(commandText, CollectParameters(parameters), Options.DefaultTransaction, Options.DefaultCommandTimeout, commandType, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return new DapperGridResultReader(reader, commandText, commandType, parameters, DbProviderAdapter, Options);
        }

        protected override IEnumerable<TReturn> Parse<TReturn>(IDataReader reader) => reader.Parse<TReturn>();

        protected override void DisposeConnection()
        {
            if (Options.OnDispose != null)
                Options.OnDispose.Invoke();
            else
                base.DisposeConnection();
        }
        #endregion

        #region Protected Methods
        protected object CollectParameters(ParametersVisitor parametersVisitor)
        {
            Guard.IsNotNull(parametersVisitor, nameof(parametersVisitor));
            DynamicParameters @params = new DynamicParameters();
            DapperParameterCollector parameterCollector = new DapperParameterCollector(@params, DbProviderAdapter);
            parametersVisitor.VisitInputParameters(parameterCollector.VisitInputParameter);
            return new DynamicParametersWrapper(@params, parametersVisitor);
        }
        #endregion

        #region Private Methods
        private static void ConfigureDapper()
        {
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());
            SqlMapper.SetTypeMap(FileEntityTypeMap.Type, new FileEntityTypeMap());
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
                _parametersVisitor.VisitOutputParameters(_impl.Get<object>);
                _parameterCallbacks.OnCompleted();
            }
        }

        private sealed class DapperParameterCollector : DbParameterCollector
        {
            private readonly DynamicParameters _dynamicParameters;

            public DapperParameterCollector(DynamicParameters dynamicParameters, DbProviderAdapter dbProviderAdapter) : base(dbProviderAdapter)
            {
                _dynamicParameters = dynamicParameters;
            }

            public override void VisitInputParameter(string name, DbType dataType, object value, int? size, bool isOutput, CustomInputType customInputType)
            {
                object normalizedValue = NormalizeParameterValue(value);
                DbType? dbType = NormalizeParameterDbType(dataType, customInputType);
                ParameterDirection? direction = isOutput ? ParameterDirection.Output : null;
                int? normalizedSize = NormalizeParameterSize(size, dbType, isOutput);
                _dynamicParameters.Add(name, value: normalizedValue, dbType, direction, normalizedSize);
            }

            private object NormalizeParameterValue(object value)
            {
                if (value is StructuredType udt)
                    return new DapperStructuredTypeParameter(udt, DbProviderAdapter);

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

        }
        #endregion
    }
}