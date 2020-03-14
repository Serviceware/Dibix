namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeResolverFacade
    {
        void Register(TypeResolver typeResolver);
        void Register(TypeResolver typeResolver, int position);
        TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable);
        TypeReference ResolveType(TypeResolutionScope scope, string input, string @namespace, string source, int line, int column, bool isEnumerable);
    }
}