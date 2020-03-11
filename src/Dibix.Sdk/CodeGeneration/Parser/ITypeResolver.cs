namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeResolver
    {
        TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable);
    }
}