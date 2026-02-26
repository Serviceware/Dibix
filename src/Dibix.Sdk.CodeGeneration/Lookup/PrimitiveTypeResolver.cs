using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    // Resolve built in primitive types
    internal sealed class PrimitiveTypeResolver : TypeResolver<PrimitiveTypeReference>
    {
        private static readonly IDictionary<string, PrimitiveType> PrimitiveTypeMap = Enum.GetValues(typeof(PrimitiveType))
                                                                                          .Cast<PrimitiveType>()
                                                                                          .Where(x => x != PrimitiveType.None)
                                                                                          .ToDictionary(x => x.ToString().ToLowerInvariant());

        public override PrimitiveTypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            if (PrimitiveTypeMap.TryGetValue(typeName.Name, out PrimitiveType primitiveType))
                return new PrimitiveTypeReference(primitiveType, typeName.IsNullable, isEnumerable, size: null, location);

            return null;
        }
    }
}