using System.Collections.Generic;

namespace Dibix.Sdk
{
    internal static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> elements)
        {
            foreach (T element in elements)
                source.Add(element);
        }

        public static ICollection<TSource> ReplaceWith<TSource>(this ICollection<TSource> source, IEnumerable<TSource> elements)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(elements, nameof(elements));

            source.Clear();
            AddRange(source, elements);

            return source;
        }
    }
}