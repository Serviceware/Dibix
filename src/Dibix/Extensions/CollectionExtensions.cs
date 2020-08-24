using System;
using System.Collections.Generic;

namespace Dibix
{
    public static class CollectionExtensions
    {
        public static ICollection<TSource> AddRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> elements)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(elements, nameof(elements));

            foreach (TSource element in elements)
                source.Add(element);

            return source;
        }

        public static ICollection<TSource> ReplaceWith<TSource>(this ICollection<TSource> source, IEnumerable<TSource> elements)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(elements, nameof(elements));

            source.Clear();
            AddRange(source, elements);

            return source;
        }

        public static IDictionary<TKey, TSource> ReplaceWith<TKey, TSource>(this IDictionary<TKey, TSource> target, IEnumerable<TSource> elements, Func<TSource, TKey> keySelector)
        {
            Guard.IsNotNull(target, nameof(target));
            Guard.IsNotNull(elements, nameof(elements));
            Guard.IsNotNull(keySelector, nameof(keySelector));

            target.Clear();
            foreach (TSource element in elements)
                target.Add(keySelector(element), element);

            return target;
        }
    }
}