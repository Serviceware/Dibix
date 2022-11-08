using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaRegistry
    {
        IEnumerable<SchemaDefinition> Schemas { get; }

        bool IsRegistered(string name);
        void Populate(SchemaDefinition schema);
        bool TryGetSchema(SchemaTypeReference reference, out SchemaDefinition schemaDefinition);
        void ImportSchemas(params ISchemaProvider[] schemaProviders);
    }
}