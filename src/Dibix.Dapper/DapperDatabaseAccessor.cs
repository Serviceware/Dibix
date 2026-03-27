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
        // We would like to change the default to false, but we cannot guarantee,
        // that consumers already extend the lifetime of their connection scope,
        // which is required in this case.
        private const bool BufferResults = true;
        #endregion

        #region Constructor
        public DapperDatabaseAccessor(DbConnection connection, DatabaseAccessorOptions options) : base(connection, options)
        {
        }

        static DapperDatabaseAccessor()
        {
            ConfigureDapper();
        }
        #endregion

        #region Overrides
        protected override int Execute(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            CommandDefinition command = PrepareCommand(commandText, commandType, parameters);
            return Connection.Execute(command);
        }

        protected override Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            CommandDefinition command = PrepareCommand(commandText, commandType, parameters, cancellationToken: cancellationToken);
            return Connection.ExecuteAsync(command);
        }

        protected override IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandDefinition command = PrepareCommand(commandText, commandType, parameters);
            return Connection.Query<T>(command);
        }

        protected override Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt<T>();
            CommandDefinition command = PrepareCommand(commandText, commandType, parameters, cancellationToken);
            return Connection.QueryAsync<T>(command);
        }

        protected override IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt(types);
            object @params = CollectParameters(parameters);
            return Connection.Query(commandText, types, map, @params, Options.DefaultTransaction, CollectBufferResultsValue(), splitOn, Options.DefaultCommandTimeout, commandType);
        }

        protected override Task<IEnumerable<TReturn>> QueryManyAsync<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, CancellationToken cancellationToken)
        {
            DecoratedTypeMap.Adapt(types);
            // NOTE: Apparently there is no overload in Dapper that either accepts CancellationToken or CommandDefinition and Type[]
            object @params = CollectParameters(parameters);
            return Connection.QueryAsync(commandText, types, map, @params, Options.DefaultTransaction, CollectBufferResultsValue(), splitOn, Options.DefaultCommandTimeout, commandType);
        }

        protected override IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            CommandDefinition commandDefinition = PrepareCommand(commandText, commandType, parameters);
            SqlMapper.GridReader reader = Connection.QueryMultiple(commandDefinition);
            return new DapperGridResultReader(reader, commandText, commandType, parameters, DbProviderAdapter, Options);
        }

        protected override async Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            CommandDefinition commandDefinition = PrepareCommand(commandText, commandType, parameters, cancellationToken);
            SqlMapper.GridReader reader = await Connection.QueryMultipleAsync(commandDefinition).ConfigureAwait(false);
            return new DapperGridResultReader(reader, commandText, commandType, parameters, DbProviderAdapter, Options);
        }

        protected override IEnumerable<TReturn> Parse<TReturn>(IDataReader reader) => reader.Parse<TReturn>();

        protected CommandDefinition PrepareCommand(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken = default)
        {
            CommandFlags flags = CollectCommandFlags();
            object @params = CollectParameters(parameters);
            CommandDefinition commandDefinition = new CommandDefinition(commandText, @params, Options.DefaultTransaction, Options.DefaultCommandTimeout, commandType, flags, cancellationToken: cancellationToken);
            return commandDefinition;
        }

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
        private CommandFlags CollectCommandFlags()
        {
            CommandFlags flags = CollectBufferResultsValue() ? CommandFlags.Buffered : CommandFlags.None;
            return flags;
        }

        private bool CollectBufferResultsValue() => Options.BufferResult ?? BufferResults;

        private static void ConfigureDapper()
        {
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());
            DecoratedTypeMap.RegisterOnce(FileEntityTypeMap.Type, (x, _) => new FileEntityTypeMap(x));
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