using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class ActionDefinitionUtility
    {
        public static TypeReference CreateStreamTypeReference(SourceLocation location) => new PrimitiveTypeReference(PrimitiveType.Stream, isNullable: false, isEnumerable: false, location);
    }
}