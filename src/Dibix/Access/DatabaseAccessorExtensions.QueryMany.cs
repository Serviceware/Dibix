using System;
using System.Collections.Generic;
using System.Data;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, commandType, EmptyParameters.Instance);
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, CommandType.Text, parameters);
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, Action<TReturn, TSecond> map, string splitOn)
        {
            return QueryMany(accessor, sql, CommandType.Text, EmptyParameters.Instance, map, splitOn);
        }
        // Workflow (GetPendingWorkflowActionRequests)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond>(sql, CommandType.Text, EmptyParameters.Instance, splitOn);
        }
        // TaskMgmt (GetUserTaskAggregates)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond>(sql, CommandType.Text, parameters, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            return QueryMany(accessor, sql, CommandType.Text, parameters, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, CommandType commandType, IParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TReturn>(sql, commandType, parameters, (a, b) =>
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

        // SubProcess (GetCmdbFlows)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, CommandType commandType, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond, TThird>(sql, commandType, EmptyParameters.Instance, splitOn);
        }
        // BPMNModeler
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            return QueryMany(accessor, sql, CommandType.Text, EmptyParameters.Instance, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            return QueryMany(accessor, sql, CommandType.Text, parameters, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, CommandType commandType, IParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TReturn>(sql, commandType, parameters, (a, b, c) =>
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

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(sql, CommandType.Text, parameters, (a, b, c, d) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d);
                return instance;
            }, splitOn);
            return cache;
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>(sql, CommandType.Text, parameters, (a, b, c, d, e) =>
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

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(sql, CommandType.Text, parameters, (a, b, c, d, e, f) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d, e, f);
                return instance;
            }, splitOn);
            return cache;
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(sql, CommandType.Text, parameters, (a, b, c, d, e, f, g, h, i) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d, e, f, g, h, i);
                return instance;
            }, splitOn);
            return cache;
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>(this IDatabaseAccessor accessor, string sql, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh> map, string splitOn)
        { 
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(sql, CommandType.Text, EmptyParameters.Instance, (a, b, c, d, e, f, g, h, i, j, k) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b, c, d, e, f, g, h, i, j, k);
                return instance;
            }, splitOn);
            return cache;
        }
    }
}