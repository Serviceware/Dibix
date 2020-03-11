using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeResolverFacade
    {
        void Register(ITypeResolver typeResolver);
        void Register(ITypeResolver typeResolver, int position);
        TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable);
    }
}