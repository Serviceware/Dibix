using System;
using System.Data;
using System.Linq;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, CommandType.Text, parameters);
        }

        // SubProcessOverview
        public static TReturn QuerySingle<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond>(sql, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TReturn>(sql, CommandType.Text, parameters, (a, b) =>
            {
                if (!cache.TryGetValue(a, out TReturn instance))
                {
                    instance = a;
                    cache.Add(instance);
                }
                map(instance, b);
                return instance;
            }, splitOn);
            return cache.Single();
        }

        public static TReturn QuerySingle<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond, TThird>(sql, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>();
            accessor.QueryMany<TReturn, TSecond, TThird, TReturn>(sql, CommandType.Text, parameters, (a, b, c) =>
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

        // OrderManagement (GetCategoryDetail)
        public static TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond, TThird, TFourth>(sql, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
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
            return cache.Single();
        }
    }
}