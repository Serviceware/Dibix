namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeResolverFacade
    {
        void Register(ITypeResolver typeResolver);
        void Register(ITypeResolver typeResolver, int position);
        TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable);
        TypeReference ResolveType(TypeResolutionScope scope, string input, string @namespace, SourceLocation location, bool isEnumerable);
    }
}