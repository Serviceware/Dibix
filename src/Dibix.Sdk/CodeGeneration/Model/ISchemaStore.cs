namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaStore
    {
        SchemaDefinition GetSchema(SchemaTypeReference schemaTypeReference);
    }
}