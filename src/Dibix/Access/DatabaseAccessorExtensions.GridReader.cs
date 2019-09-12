using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        // ObjectManagement (GetDetailConfigurationExportById, GetDetailConfigurationExportByObjectDef)
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond>(this IMultipleResultReader reader, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(reader, nameof(reader));
            MultiMapper multiMapper = new MultiMapper();
            return reader.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                         .PostProcess(multiMapper);
        }
        public static IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(this IMultipleResultReader reader, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(reader, nameof(reader));
            MultiMapper multiMapper = new MultiMapper();
            return reader.ReadMany<TFirst, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(true, a, b), splitOn)
                         .PostProcess(multiMapper);
        }
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
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
        // OrderManagement (GetProduct)
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth>(this IMultipleResultReader reader, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(reader, nameof(reader));
            MultiMapper multiMapper = new MultiMapper();
            return reader.ReadMany<TReturn, TSecond, TThird, TFourth, TReturn>((a, b, c, d) => multiMapper.MapRow<TReturn>(false, a, b, c, d), splitOn)
                         .PostProcess(multiMapper);
        }
        public static IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird, TFourth, TFifth> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
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

        public static TReturn ReadSingle<TReturn, TSecond>(this IMultipleResultReader reader, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(reader, nameof(reader));
            MultiMapper multiMapper = new MultiMapper();
            return reader.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(false, a, b), splitOn)
                         .PostProcess(multiMapper)
                         .Single();
        }
        public static TReturn ReadSingle<TReturn, TSecond, TThird>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
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

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
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
        // OrderManagement (GetProduct)
        public static TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth>(this IMultipleResultReader reader, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(reader, nameof(reader));
            MultiMapper multiMapper = new MultiMapper();
            return reader.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>((a, b, c, d, e) => multiMapper.MapRow<TReturn>(false, a, b, c, d, e), splitOn)
                         .PostProcess(multiMapper)
                         .Single();
        }
        public static TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(this IMultipleResultReader reader, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth> map, string splitOn)
        {
            Guard.IsNotNull(reader, nameof(reader));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
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