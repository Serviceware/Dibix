using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumerableExtensions
    {
        public static void Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action(element);
        }
    }
}
