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
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, commandType, EmptyParameters.Instance);
        }
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, CommandType.Text, configureParameters.Build());
        }
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, commandType, configureParameters.Build());
        }
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(sql, CommandType.Text, parameters);
        }
        public static TReturn QuerySingle<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters, Action<TReturn, TSecond> map, string splitOn) => QuerySingle(accessor, sql, configureParameters.Build(), map, splitOn);
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