namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaDefinitionResolver : ISchemaStore
    {
        SchemaDefinition Resolve(SchemaTypeReference schemaTypeReference);
    }
}