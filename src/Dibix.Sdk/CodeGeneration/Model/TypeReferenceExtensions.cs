namespace Dibix.Sdk.CodeGeneration
{
    internal static class TypeReferenceExtensions
    {
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver) => IsUserDefinedType(typeReference, schemaDefinitionResolver, out UserDefinedTypeSchema _);
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out UserDefinedTypeSchema userDefinedTypeSchema)
        {
            if (typeReference is SchemaTypeReference schemaTypeReference)
            {
                userDefinedTypeSchema = schemaDefinitionResolver.Resolve(schemaTypeReference) as UserDefinedTypeSchema;
                return userDefinedTypeSchema != null;
            }
            userDefinedTypeSchema = null;
            return false;
        }
    }
}