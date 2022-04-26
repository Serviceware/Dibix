namespace Dibix.Sdk.CodeGeneration
{
    internal static class RawValueReferenceParser
    {
        public static ValueReference Parse(TypeReference targetType, object value, string filePath, int line, int column, ILogger logger)
        {
            if (value == null)
                return new NullValueReference(targetType, filePath, line, column);

            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    return new PrimitiveValueReference(primitiveTypeReference, value, filePath, line, column);

                case SchemaTypeReference schemaTypeReference:
                    int intValue = (int)value;
                    return new EnumMemberNumericReference(schemaTypeReference, intValue, filePath, line, column);

                default:
                    logger.LogError($"Unexpected target type for constant value: {targetType?.GetType()}", filePath, line, column);
                    return null;
            }
        }
    }
}