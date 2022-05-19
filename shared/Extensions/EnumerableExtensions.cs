using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            return source.Distinct().Where(element => seenKeys.Add(keySelector(element)));
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int i = 0;
            foreach (T element in source)
                action(element, i++);
        }
        public static void Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action(element);
        }

        // Array is allocated and the enumerable is not enumerated lazy
        //public static IEnumerable<T> Create<T>(params T[] args) => args;
        public static IEnumerable<T> Create<T>(T arg1)
        {
            yield return arg1;
        }
        public static IEnumerable<T> Create<T>(T arg1, T arg2)
        {
            yield return arg1;
            yield return arg2;
        }
        public static IEnumerable<T> Create<T>(T arg1, T arg2, T arg3)
        {
            yield return arg1;
            yield return arg2;
            yield return arg3;
        }
        public static IEnumerable<T> Create<T>(T arg1, T arg2, T arg3, T arg4)
        {
            yield return arg1;
            yield return arg2;
            yield return arg3;
            yield return arg4;
        }
        public static IEnumerable<T> Create<T>(T arg1, T arg2, T arg3, T arg4, T arg5)
        {
            yield return arg1;
            yield return arg2;
            yield return arg3;
            yield return arg4;
            yield return arg5;
        }
    }
}