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
        IEnumerable<TReturn> ReadMany<TReturn>(Type[] types, string splitOn) where TReturn : new();
        T ReadSingle<T>();
        Task<T> ReadSingleAsync<T>();
        TReturn ReadSingle<TReturn>(Type[] types, string splitOn) where TReturn : new();
        T ReadSingleOrDefault<T>();
        Task<T> ReadSingleOrDefaultAsync<T>();
        TReturn ReadSingleOrDefault<TReturn>(Type[] types, string splitOn) where TReturn : new();
    }
}