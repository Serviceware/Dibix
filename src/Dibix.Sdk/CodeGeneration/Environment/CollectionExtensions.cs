using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
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