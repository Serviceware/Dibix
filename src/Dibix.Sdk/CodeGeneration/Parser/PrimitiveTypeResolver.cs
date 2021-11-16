using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PrimitiveTypeResolver : TypeResolver
    {
        public override TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            if (Enum.TryParse(typeName.Name, ignoreCase: true /* JSON is camelCase while C# is PascalCase */, out PrimitiveType primitiveType))
                return new PrimitiveTypeReference(primitiveType, typeName.IsNullable, isEnumerable, source, line, column);

            return null;
        }
    }
}