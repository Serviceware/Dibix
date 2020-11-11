using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, CommandType.Text, EmptyParameters.Instance);
        }
        // DataImport (LoadDeployAlias)
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, commandType, EmptyParameters.Instance);
        }
        // Configurator (GetKnowledgeBaseServiceConfiguration)
        public static Task<T> QuerySingleAsync<T>(this IDatabaseAccessor accessor, string sql, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleAsync<T>(sql, CommandType.Text, EmptyParameters.Instance, cancellationToken);
        }
        public static T QuerySingleOrDefault<T>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingleOrDefault<T>(sql, CommandType.Text, parameters);
        }

        public static TReturn QuerySingleOrDefault<TReturn, TSecond>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
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
            return cache.SingleOrDefault();
        }

        // BPMNModeler
        public static TReturn QuerySingleOrDefault<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
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
            return cache.SingleOrDefault();
        }

        public static TReturn QuerySingleOrDefault<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
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
            return cache.SingleOrDefault();
        }
    }
}