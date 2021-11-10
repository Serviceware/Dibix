using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaRegistry
    {
        IEnumerable<SchemaDefinition> Schemas { get; }

        bool IsRegistered(string name);
        void Populate(SchemaDefinition schema);
        SchemaDefinition GetSchema(SchemaTypeReference reference);
        void ImportSchemas(params ISchemaProvider[] schemaProviders);
    }
}