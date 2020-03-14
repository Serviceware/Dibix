namespace Dibix.Sdk.CodeGeneration
{
    public abstract class TypeResolver
    {
        public virtual TypeResolutionScope Scope => TypeResolutionScope.All;

        public abstract TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable);
    }
}