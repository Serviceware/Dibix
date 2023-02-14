namespace Dibix.Sdk.CodeGeneration
{
    internal static class TypeReferenceExtensions
    {
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaRegistry schemaRegistry) => typeReference.IsUserDefinedType(schemaRegistry, out UserDefinedTypeSchema _);
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaRegistry schemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema) => typeReference.IsSchema(schemaRegistry, out userDefinedTypeSchema);

        public static bool IsEnum(this TypeReference typeReference, ISchemaRegistry schemaRegistry) => typeReference.IsEnum(schemaRegistry, out EnumSchema _);
        public static bool IsEnum(this TypeReference typeReference, ISchemaRegistry schemaRegistry, out EnumSchema enumSchema) => typeReference.IsSchema(schemaRegistry, out enumSchema);
        public static bool IsEnum(this SchemaTypeReference schemaTypeReference, ISchemaRegistry schemaRegistry) => schemaTypeReference.IsSchema(schemaRegistry, out EnumSchema _);
        public static bool IsEnum(this SchemaTypeReference schemaTypeReference, ISchemaRegistry schemaRegistry, out EnumSchema enumSchema) => schemaTypeReference.IsSchema(schemaRegistry, out enumSchema);

        public static bool IsSchema<TSchema>(this TypeReference typeReference, ISchemaRegistry schemaRegistry, out TSchema schema) where TSchema : SchemaDefinition
        {
            if (typeReference is SchemaTypeReference schemaTypeReference)
            {
                schema = schemaRegistry.GetSchema(schemaTypeReference) as TSchema;
                return schema != null;
            }
            schema = null;
            return false;
        }
        public static bool IsSchema<TSchema>(this SchemaTypeReference schemaTypeReference, ISchemaRegistry schemaRegistry, out TSchema schema) where TSchema : SchemaDefinition
        {
            schema = schemaRegistry.GetSchema(schemaTypeReference) as TSchema;
            return schema != null;
        }
    }
}