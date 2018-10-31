using System;
using System.Collections.Generic;

namespace Dibix
{
    public interface IMultipleResultReader : IDisposable
    {
        IEnumerable<T> ReadMany<T>();
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);
        T ReadSingle<T>();
    }
}