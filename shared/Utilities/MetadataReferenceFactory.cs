using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Dibix
{
    internal static class MetadataReferenceFactory
    {
        public static MetadataReference FromType<T>() => FromAssembly(typeof(T).Assembly);

        public static MetadataReference FromAssembly(Assembly assembly) => MetadataReference.CreateFromFile(assembly.Location);
    }
}