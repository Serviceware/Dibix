using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> elements)
        {
            foreach (T element in elements)
                source.Add(element);
        }
    }
}
