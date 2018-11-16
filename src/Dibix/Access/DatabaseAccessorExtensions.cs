using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dibix
{
    public static class DatabaseAccessorExtensions
    {
        private const string DefaultSplitOn = "Id";

        public static int Execute(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, EmptyParameters.Instance);
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, configureParameters.Build());
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, configureParameters.Build());
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, parameters);
        }

        public static T ExecuteScalar<T>(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteScalar<T>(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static T ExecuteScalar<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteScalar<T>(sql, commandType, EmptyParameters.Instance);
        }
        public static T ExecuteScalar<T>(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteScalar<T>(sql, CommandType.Text, configureParameters.Build());
        }
        public static T ExecuteScalar<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteScalar<T>(sql, commandType, configureParameters.Build());
        }
        public static T ExecuteScalar<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteScalar<T>(sql, CommandType.Text, parameters);
        }

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
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, CommandType.Text, configureParameters.Build());
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, commandType, configureParameters.Build());
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(sql, CommandType.Text, parameters);
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, Action<TReturn, TSecond> map)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return QueryMany(accessor, sql, EmptyParameters.Instance, map, DefaultSplitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return QueryMany(accessor, sql, EmptyParameters.Instance, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond> map)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return QueryMany(accessor, sql, parameters, map, DefaultSplitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
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
            return cache;
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
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
            return cache;
        }

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
        public static TReturn QuerySingleOrDefault<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
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
            return cache.SingleOrDefault();
        }

        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, commandType, EmptyParameters.Instance);
        }
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, CommandType.Text, configureParameters.Build());
        }
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, commandType, configureParameters.Build());
        }
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, CommandType.Text, parameters);
        }

        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, commandType, EmptyParameters.Instance);
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, configureParameters.Build());
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, commandType, configureParameters.Build());
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, parameters);
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

        private static IParametersVisitor Build(this Action<IParameterBuilder> configureParameters)
        {
            IParameterBuilder builder = new ParameterBuilder();
            configureParameters(builder);
            return builder.Build();
        }

        private sealed class EmptyParameters : IParametersVisitor
        {
            private static readonly EmptyParameters CachedInstance = new EmptyParameters();
            private EmptyParameters() { }

            public static IParametersVisitor Instance => CachedInstance;

            void IParametersVisitor.VisitParameters(ParameterVisitor visitParameter)
            {
                // No parameters so nothing to do here
            }
        }
    }
}