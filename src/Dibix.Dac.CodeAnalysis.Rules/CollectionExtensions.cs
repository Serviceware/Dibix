using System.Collections.Generic;

namespace Dibix.Dac.CodeAnalysis.Rules
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
