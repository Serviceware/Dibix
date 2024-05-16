using System;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class JsonExtensions
    {
        public static T ResolveType<T>(this JValue typeNameValue, TypeResolver<T> typeResolver) where T : TypeReference => ResolveType(typeNameValue, (string)typeNameValue, typeResolver.ResolveType);
        public static TypeReference ResolveType(this JValue typeNameValue, ITypeResolverFacade typeResolver) => ResolveType(typeNameValue, (string)typeNameValue, typeResolver);
        private static TypeReference ResolveType(JValue typeNameValue, string typeName, ITypeResolverFacade typeResolver) => ResolveType(typeNameValue, typeName, typeResolver.ResolveType);
        private static T ResolveType<T>(JValue typeNameValue, string typeName, Func<string, string, SourceLocation, bool, T> typeResolver) where T : TypeReference
        {
            Guard.IsNotNull(typeNameValue, nameof(typeNameValue));
            SourceLocation typeNameLocation = typeNameValue.GetSourceInfo();
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            T type = typeResolver(typeName, null, typeNameLocation, isEnumerable);
            return type;
        }
    }
}