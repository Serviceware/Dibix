using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class RawValueReferenceParser
    {
        public static ValueReference Parse(TypeReference targetType, object value, SourceLocation location, ILogger logger)
        {
            if (value == null)
                return new NullValueReference(targetType, location);

            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    return new PrimitiveValueReference(primitiveTypeReference, value, location);

                case SchemaTypeReference schemaTypeReference:
                    int intValue = (int)value;
                    return new EnumMemberNumericReference(schemaTypeReference, intValue, location);

                default:
                    logger.LogError($"Unexpected target type for constant value: {targetType?.GetType()}", location.Source, location.Line, location.Column);
                    return null;
            }
        }
    }
}