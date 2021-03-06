using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond>(this IMultipleResultReader reader, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TReturn>((a, b) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b);
                return instance;
            }, splitOn);
            return cache;
        }
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TThird, TReturn>((a, b, c) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c);
                return instance;
            }, splitOn);
            return cache;
        }
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird, TFourth, TFifth> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>((a, b, c, d, e) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d, e);
                return instance;
            }, splitOn);
            return cache;
        }

        public static TReturn ReadSingle<TReturn, TSecond, TThird>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TThird, TReturn>((a, b, c) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c);
                return instance;
            }, splitOn);
            return cache.Single();
        }
        public static TReturn ReadSingle<TReturn, TSecond, TThird, TFourth>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TThird, TFourth, TReturn>((a, b, c, d) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d);
                return instance;
            }, splitOn);
            return cache.Single();
        }
        public static TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            reader.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>((a, b, c, d, e, f) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d, e, f);
                return instance;
            }, splitOn);
            return cache.Single();
        }
    }
}