using System;
using System.Collections.Generic;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperGridResultReader : IMultipleResultReader, IDisposable
    {
        #region Fields
        private readonly SqlMapper.GridReader _reader;
        private readonly DapperMappingCheck _mappingCheck;
        #endregion

        #region Constructor
        public DapperGridResultReader(SqlMapper.GridReader reader, DapperMappingCheck mappingCheck)
        {
            this._reader = reader;
            this._mappingCheck = mappingCheck;
        }
        #endregion

        #region IMultipleResultReader Members
        public IEnumerable<T> ReadMany<T>()
        {
            this._mappingCheck.Check<T>();
            return this._reader.Read<T>();
        }

        public IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird>();
            return this._reader.Read(map, splitOn);
        }

        public IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            this._mappingCheck.Check<TFirst, TSecond, TThird, TFourth>();
            return this._reader.Read(map, splitOn);
        }

        public T ReadSingle<T>()
        {
            this._mappingCheck.Check<T>();
            return this._reader.ReadSingle<T>();
        }

        public T ReadSingleOrDefault<T>()
        {
            this._mappingCheck.Check<T>();
            return this._reader.ReadSingleOrDefault<T>();
        }
        #endregion

        #region IDisposable Members
        void IDisposable.Dispose()
        {
            this._reader?.Dispose();
        }
        #endregion
    }
}