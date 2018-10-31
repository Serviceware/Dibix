using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
            foreach (T element in enumerable)
                action(element);

            return enumerable;
        }
    }
}
