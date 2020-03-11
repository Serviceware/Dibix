namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaRegistry
    {
        bool IsRegistered(string name);
        void Populate(SchemaDefinition schema);
        SchemaDefinition GetSchema(SchemaTypeReference reference);
        void ImportSchemas(params ISchemaProvider[] schemaProviders);
    }
}