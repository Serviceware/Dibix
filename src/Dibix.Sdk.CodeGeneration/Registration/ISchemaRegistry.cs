using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaRegistry
    {
        IEnumerable<SchemaDefinition> Schemas { get; }

        bool IsRegistered(string fullName);
        void Populate(SchemaDefinition schema);
        SchemaDefinition GetSchema(SchemaTypeReference schemaTypeReference);
        TSchema GetSchema<TSchema>(SchemaTypeReference schemaTypeReference) where TSchema : SchemaDefinition;
        bool TryGetSchema<TSchema>(string name, out TSchema schema) where TSchema : SchemaDefinition;
        void ImportSchemas(params ISchemaProvider[] schemaProviders);
    }
}