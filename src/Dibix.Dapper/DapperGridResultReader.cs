using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperGridResultReader : MultipleResultReader, IMultipleResultReader, IDisposable
    {
        #region Fields
        private readonly SqlMapper.GridReader _reader;
        #endregion

        #region Properties
        public override bool IsConsumed => _reader.IsConsumed;
        #endregion

        #region Constructor
        public DapperGridResultReader(SqlMapper.GridReader reader, string commandText, CommandType commandType, ParametersVisitor parameters) : base(commandText, commandType, parameters, isSqlClient: reader.Command is SqlCommand)
        {
            _reader = reader;
        }
        #endregion

        #region IMultipleResultReader Members
        protected override IEnumerable<T> ReadMany<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.Read<T>();
        }

        protected override Task<IEnumerable<T>> ReadManyAsync<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadAsync<T>();
        }

        protected override IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt(types);
            return _reader.Read(types, map, splitOn);
        }

        protected override T ReadSingle<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadSingle<T>();
        }

        protected override Task<T> ReadSingleAsync<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadSingleAsync<T>();
        }

        protected override T ReadSingleOrDefault<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadSingleOrDefault<T>();
        }

        protected override Task<T> ReadSingleOrDefaultAsync<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadSingleOrDefaultAsync<T>();
        }
        #endregion

        #region IDisposable Members
        public override void Dispose()
        {
            _reader?.Dispose();
        }
        #endregion
    }
}