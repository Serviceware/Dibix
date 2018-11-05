using System;
using System.Collections.Generic;

namespace Dibix
{
    public static class StructuredTypeExtensions
    {
        public static TStructuredType AddRange<TStructuredType, T>(this TStructuredType target, IEnumerable<T> source, Action<TStructuredType, T> collector) where TStructuredType : StructuredType, new()
        {
            Guard.IsNotNull(target, nameof(target));
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(collector, nameof(collector));

            foreach (T item in source)
            {
                collector(target, item);
            }
            return target;
        }

    }
}