namespace Dibix.Sdk.CodeGeneration
{
    internal static class TypeReferenceExtensions
    {
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver) => typeReference.IsUserDefinedType(schemaDefinitionResolver, out UserDefinedTypeSchema _);
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out UserDefinedTypeSchema userDefinedTypeSchema) => typeReference.IsSchema(schemaDefinitionResolver, out userDefinedTypeSchema);

        public static bool IsEnum(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver) => typeReference.IsEnum(schemaDefinitionResolver, out EnumSchema _);
        public static bool IsEnum(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out EnumSchema enumSchema) => typeReference.IsSchema(schemaDefinitionResolver, out enumSchema);
        public static bool IsEnum(this SchemaTypeReference schemaTypeReference, ISchemaDefinitionResolver schemaDefinitionResolver) => schemaTypeReference.IsSchema(schemaDefinitionResolver, out EnumSchema _);
        public static bool IsEnum(this SchemaTypeReference schemaTypeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out EnumSchema enumSchema) => schemaTypeReference.IsSchema(schemaDefinitionResolver, out enumSchema);

        public static bool IsSchema<TSchema>(this TypeReference typeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out TSchema schema) where TSchema : SchemaDefinition
        {
            if (typeReference is SchemaTypeReference schemaTypeReference)
            {
                schema = schemaDefinitionResolver.Resolve(schemaTypeReference) as TSchema;
                return schema != null;
            }
            schema = null;
            return false;
        }
        public static bool IsSchema<TSchema>(this SchemaTypeReference schemaTypeReference, ISchemaDefinitionResolver schemaDefinitionResolver, out TSchema schema) where TSchema : SchemaDefinition
        {
            schema = schemaDefinitionResolver.Resolve(schemaTypeReference) as TSchema;
            return schema != null;
        }
    }
}