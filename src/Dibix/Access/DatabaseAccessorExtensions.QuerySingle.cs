using System;
using System.Data;
using System.Linq;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string commandText)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(commandText, CommandType.Text, ParametersVisitor.Empty);
        }
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(commandText, CommandType.Text, parameters);
        }
        // QueryServerProductVersion (DataImport.Data.DML.IntegrationServices)
        public static T QuerySingle<T>(this IDatabaseAccessor accessor, string commandText, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<T>(commandText, commandType, ParametersVisitor.Empty);
        }

        // SubProcessOverview
        public static TReturn QuerySingle<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond>(commandText, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TReturn>(commandText, CommandType.Text, parameters, (a, b) =>
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

        public static TReturn QuerySingle<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond, TThird>(commandText, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TReturn>(commandText, CommandType.Text, parameters, (a, b, c) =>
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
        public static TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QuerySingle<TReturn, TSecond, TThird, TFourth>(commandText, CommandType.Text, parameters, splitOn);
        }
        public static TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(commandText, CommandType.Text, parameters, (a, b, c, d) =>
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