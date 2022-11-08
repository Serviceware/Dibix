namespace Dibix.Sdk.CodeGeneration
{
    internal static class ActionDefinitionUtility
    {
        public static TypeReference CreateStreamTypeReference(string source, int line, int column) => new PrimitiveTypeReference(PrimitiveType.Stream, isNullable: false, isEnumerable: false, source, line, column);
    }
}