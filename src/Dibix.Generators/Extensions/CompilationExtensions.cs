using System;
using Microsoft.CodeAnalysis;

namespace Dibix.Generators
{
    internal static class EnumerableExtensions
    {
        public static INamedTypeSymbol GetTypeByMetadataNameSafe(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            if (symbol == null)
                throw new InvalidOperationException($"Could not resolve symbol: {fullyQualifiedMetadataName}");

            return symbol;
        }
    }
}