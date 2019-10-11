using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    public abstract class MultipleResultReader : IMultipleResultReader, IDisposable
    {
        #region IMultipleResultReader Members
        public abstract bool IsConsumed { get; }

        IEnumerable<T> IMultipleResultReader.ReadMany<T>() => this.ReadMany<T>().PostProcess();

        // ObjectManagement (GetDetailConfigurationExportById, GetDetailConfigurationExportByObjectDef)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond>(string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper);
        }

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn) => this.ReadMany(map, splitOn).PostProcess();

        public IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TFirst, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(true, a, b), splitOn)
                       .PostProcess(multiMapper);
        }

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn) => this.ReadMany(map, splitOn).PostProcess();

        // OrderManagement (GetProduct)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth>(string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TReturn>((a, b, c, d) => multiMapper.MapRow<TReturn>(false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper);
        }

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn) => this.ReadMany(map, splitOn).PostProcess();

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn) => this.ReadMany(map, splitOn).PostProcess();

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn) => this.ReadMany(map, splitOn).PostProcess();

        T IMultipleResultReader.ReadSingle<T>() => this.ReadSingle<T>().PostProcess();

        public TReturn ReadSingle<TReturn, TSecond>(string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        }

        // OrderManagement (GetProduct)
        public TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth>(string splitOn) where TReturn : new()
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>((a, b, c, d, e) => multiMapper.MapRow<TReturn>(false, a, b, c, d, e), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        }

        T IMultipleResultReader.ReadSingleOrDefault<T>() => this.ReadSingleOrDefault<T>().PostProcess();
        #endregion
        
        #region Abstract Methods
        protected abstract IEnumerable<T> ReadMany<T>();

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);

        protected abstract T ReadSingle<T>();

        protected abstract T ReadSingleOrDefault<T>();
        #endregion

        #region IDisposable Members
        public abstract void Dispose();
        #endregion
    }
}