namespace Dibix.Sdk.CodeGeneration.Model
{
    public interface ISchemaStore
    {
        bool TryGetSchema(string fullName, out SchemaDefinition schema);
    }
}