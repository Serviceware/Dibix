using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dibix
{
    public interface IMultipleResultReader : IDisposable
    {
        bool IsConsumed { get; }

        IEnumerable<T> ReadMany<T>();
        Task<IEnumerable<T>> ReadManyAsync<T>();
        IEnumerable<TReturn> ReadMany<TReturn, TSecond>(string splitOn) where TReturn : new();
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn);
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(string splitOn) where TReturn : new();
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth>(string splitOn) where TReturn : new();
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);
        IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(string splitOn) where TReturn : new();
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);
        T ReadSingle<T>();
        Task<T> ReadSingleAsync<T>();
        TReturn ReadSingle<TReturn, TSecond>(string splitOn) where TReturn : new();
        TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth>(string splitOn) where TReturn : new();
        TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth>(string splitOn) where TReturn : new();
        T ReadSingleOrDefault<T>();
        TReturn ReadSingleOrDefault<TReturn, TSecond>(string splitOn) where TReturn : new();
        TReturn ReadSingleOrDefault<TReturn, TSecond, TThird>(string splitOn) where TReturn : new();
    }
}