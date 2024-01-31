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
        protected override IEnumerable<T> ReadMany<T>(bool buffered)
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.Read<T>(buffered);
        }

        protected override Task<IEnumerable<T>> ReadManyAsync<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadAsync<T>();
        }
        protected override Task<IEnumerable<T>> ReadManyAsync<T>(bool buffered)
        {
            DecoratedTypeMap.Adapt<T>();
            return _reader.ReadAsync<T>(buffered);
        }

        protected override IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered)
        {
            DecoratedTypeMap.Adapt(types);
            return _reader.Read(types, map, splitOn, buffered);
        }

        // NOTE: Apparently there is no async overload in Dapper using multimap
        //protected override Task<IEnumerable<TReturn>> ReadManyAsync<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered)
        //{
        //    DecoratedTypeMap.Adapt(types);
        //    return _reader.ReadAsync(types, map, splitOn, buffered);
        //}
        #endregion

        #region IDisposable Members
        public override void Dispose()
        {
            _reader?.Dispose();
        }
        #endregion
    }
}