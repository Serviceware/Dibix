namespace Dibix.Sdk.CodeGeneration
{
    public abstract class TypeResolver<TTypeReference> : ITypeResolver where TTypeReference : TypeReference
    {
        public virtual TypeResolutionScope Scope => TypeResolutionScope.All;

        TypeReference ITypeResolver.ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable) => ResolveType(input, relativeNamespace, location, isEnumerable);

        public abstract TTypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable);
    }

    public interface ITypeResolver
    {
        TypeResolutionScope Scope { get; }

        TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable);
    }
}