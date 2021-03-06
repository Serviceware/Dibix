using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string commandText)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(commandText, CommandType.Text, ParametersVisitor.Empty);
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string commandText, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(commandText, commandType, ParametersVisitor.Empty);
        }
        public static IEnumerable<T> QueryMany<T>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<T>(commandText, CommandType.Text, parameters);
        }

        public static Task<IEnumerable<T>> QueryManyAsync<T>(this IDatabaseAccessor accessor, string commandText, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryManyAsync<T>(commandText, CommandType.Text, ParametersVisitor.Empty, buffered: true, cancellationToken);
        }
        // Notification (GetDeadLetters)
        public static Task<IEnumerable<T>> QueryManyAsync<T>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryManyAsync<T>(commandText, CommandType.Text, parameters, buffered: true, cancellationToken);
        }
        // Notification (GetRetryRequests)
        public static Task<IEnumerable<T>> QueryManyAsync<T>(this IDatabaseAccessor accessor, string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryManyAsync<T>(commandText, commandType, parameters, buffered: true, cancellationToken);
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, Action<TReturn, TSecond> map, string splitOn)
        {
            return QueryMany(accessor, commandText, CommandType.Text, ParametersVisitor.Empty, map, splitOn);
        }
        // Workflow (GetPendingWorkflowActionRequests)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond>(commandText, CommandType.Text, ParametersVisitor.Empty, splitOn);
        }
        // SubProcess (GetAttributesOfCmdbFlows)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, CommandType commandType, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond>(commandText, commandType, ParametersVisitor.Empty, splitOn);
        }
        // TaskMgmt (GetUserTaskAggregates)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond>(commandText, CommandType.Text, parameters, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            return QueryMany(accessor, commandText, CommandType.Text, parameters, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond>(this IDatabaseAccessor accessor, string commandText, CommandType commandType, ParametersVisitor parameters, Action<TReturn, TSecond> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TReturn>(commandText, commandType, parameters, (a, b) =>
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

        // Workflow (GetPendingWorkflowInstanceActionRequests)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond, TThird>(commandText, CommandType.Text, ParametersVisitor.Empty, splitOn);
        }
        // Notifications (GetNotificationWebRequest)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond, TThird>(commandText, CommandType.Text, parameters, splitOn);
        }
        // SubProcess (GetCmdbFlows)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, CommandType commandType, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond, TThird>(commandText, commandType, ParametersVisitor.Empty, splitOn);
        }
        // BPMNModeler
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            return QueryMany(accessor, commandText, CommandType.Text, ParametersVisitor.Empty, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            return QueryMany(accessor, commandText, CommandType.Text, parameters, map, splitOn);
        }
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(this IDatabaseAccessor accessor, string commandText, CommandType commandType, ParametersVisitor parameters, Action<TReturn, TSecond, TThird> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TReturn>(commandText, commandType, parameters, (a, b, c) =>
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

        // News (GetEditorialGraph)
        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, string splitOn) where TReturn : new()
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMany<TReturn, TSecond, TThird, TFourth>(commandText, CommandType.Text, parameters, splitOn);
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth> map, string splitOn)
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
            return cache;
        }

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>(commandText, CommandType.Text, parameters, (a, b, c, d, e) =>
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

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(commandText, CommandType.Text, parameters, (a, b, c, d, e, f) =>
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

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth> map, string splitOn)
        {
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(commandText, CommandType.Text, parameters, (a, b, c, d, e, f, g, h, i) =>
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

        public static IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>(this IDatabaseAccessor accessor, string commandText, Action<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh> map, string splitOn)
        { 
            Guard.IsNotNull(accessor, nameof(accessor));

            HashCollection<TReturn> cache = new HashCollection<TReturn>(EntityEqualityComparer<TReturn>.Create());
            accessor.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(commandText, CommandType.Text, ParametersVisitor.Empty, (a, b, c, d, e, f, g, h, i, j, k) =>
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