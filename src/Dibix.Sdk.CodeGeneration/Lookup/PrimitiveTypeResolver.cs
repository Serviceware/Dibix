﻿using System;

namespace Dibix.Sdk.CodeGeneration
{
    // Resolve built in primitive types
    internal sealed class PrimitiveTypeResolver : TypeResolver<PrimitiveTypeReference>
    {
        public override PrimitiveTypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            if (Enum.TryParse(typeName.Name, ignoreCase: true /* JSON is camelCase while C# is PascalCase */, out PrimitiveType primitiveType))
                return new PrimitiveTypeReference(primitiveType, typeName.IsNullable, isEnumerable, size: null, location);

            return null;
        }
    }
}