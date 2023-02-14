using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class TypeResolver
    {
        public virtual TypeResolutionScope Scope => TypeResolutionScope.All;

        public abstract TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable);
    }
}