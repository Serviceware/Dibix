using Microsoft.CodeAnalysis;

namespace Dibix
{
    internal static class MetadataReferenceFactory
    {
        public static MetadataReference FromType<T>() => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);
    }
}