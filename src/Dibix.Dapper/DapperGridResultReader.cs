using System;
using System.Collections.Generic;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperGridResultReader : MultipleResultReader, IMultipleResultReader, IDisposable
    {
        #region Fields
        private readonly SqlMapper.GridReader _reader;
        #endregion

        #region Properties
        public override bool IsConsumed => this._reader.IsConsumed;
        #endregion

        #region Constructor
        public DapperGridResultReader(SqlMapper.GridReader reader)
        {
            this._reader = reader;
        }
        #endregion

        #region IMultipleResultReader Members
        protected override IEnumerable<T> ReadMany<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return this._reader.Read<T>();
        }

        protected override IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond>();
            return this._reader.Read(map, splitOn);
        }

        protected override IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird>();
            return this._reader.Read(map, splitOn);
        }

        protected override IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth>();
            return this._reader.Read(map, splitOn);
        }

        protected override IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth>();
            return this._reader.Read(map, splitOn);
        }

        protected override IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn)
        {
            DecoratedTypeMap.Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>();
            return this._reader.Read(map, splitOn);
        }

        protected override T ReadSingle<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return this._reader.ReadSingle<T>();
        }

        protected override T ReadSingleOrDefault<T>()
        {
            DecoratedTypeMap.Adapt<T>();
            return this._reader.ReadSingleOrDefault<T>();
        }
        #endregion

        #region IDisposable Members
        public override void Dispose()
        {
            this._reader?.Dispose();
        }
        #endregion
    }
}