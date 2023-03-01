using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    // Resolve schema by their UDT name
    public sealed class UserDefinedTypeSchemaTypeResolver : TypeResolver
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly Lazy<IDictionary<string, UserDefinedTypeSchema>> _schemaAccessor;

        public override TypeResolutionScope Scope => TypeResolutionScope.UserDefinedType;

        public UserDefinedTypeSchemaTypeResolver(ISchemaRegistry schemaRegistry)
        {
            _schemaRegistry = schemaRegistry;
            _schemaAccessor = new Lazy<IDictionary<string, UserDefinedTypeSchema>>(CollectSchemas);
        }

        public override TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable)
        {
            if (!_schemaAccessor.Value.TryGetValue(input, out UserDefinedTypeSchema schema)) 
                return null;

            schema.ReferenceCount++;
            SchemaTypeReference schemaTypeReference = new SchemaTypeReference(schema.FullName, isNullable: false, isEnumerable: false, location);
            return schemaTypeReference;
        }

        private IDictionary<string, UserDefinedTypeSchema> CollectSchemas() => _schemaRegistry.Schemas.OfType<UserDefinedTypeSchema>().ToDictionary(x => x.UdtName);
    }
}