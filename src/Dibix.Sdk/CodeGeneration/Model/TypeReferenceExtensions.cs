namespace Dibix.Sdk.CodeGeneration
{
    internal static class TypeReferenceExtensions
    {
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaRegistry schemaRegistry) => IsUserDefinedType(typeReference, schemaRegistry, out UserDefinedTypeSchema _);
        public static bool IsUserDefinedType(this TypeReference typeReference, ISchemaRegistry schemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema)
        {
            if (typeReference is SchemaTypeReference schemaTypeReference)
            {
                userDefinedTypeSchema = schemaRegistry.GetSchema(schemaTypeReference) as UserDefinedTypeSchema;
                return userDefinedTypeSchema != null;
            }
            userDefinedTypeSchema = null;
            return false;
        }

        public static bool IsEnum(this TypeReference typeReference, ISchemaRegistry schemaRegistry, out EnumSchema enumSchema)
        {
            if (typeReference is SchemaTypeReference schemaTypeReference)
            {
                enumSchema = schemaRegistry.GetSchema(schemaTypeReference) as EnumSchema;
                return enumSchema != null;
            }
            enumSchema = null;
            return false;
        }
    }
}