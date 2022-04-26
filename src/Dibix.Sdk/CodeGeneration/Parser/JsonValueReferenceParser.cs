using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class JsonValueReferenceParser
    {
        public static ValueReference Parse(TypeReference targetType, JValue value, string filePath, IJsonLineInfo location, ILogger logger)
        {
            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    if (TryParseValue(value, value.Type, primitiveTypeReference.Type, out object rawValue))
                        return new PrimitiveValueReference(primitiveTypeReference, rawValue, filePath, location.LineNumber, location.LinePosition);

                    logger.LogError($"Could not convert value '{value}' to type '{primitiveTypeReference.Type}'", filePath, location.LineNumber, location.LinePosition);
                    return null;

                case SchemaTypeReference schemaTypeReference:
                    return ParseEnumValue(value, value.Type, schemaTypeReference, filePath, location, logger);

                default:
                    logger.LogError($"Unexpected target type for constant value: {targetType?.GetType()}", filePath, location.LineNumber, location.LinePosition);
                    return null;
            }
        }

        private static bool TryParseValue(JValue jsonValue, JTokenType sourceType, PrimitiveType targetType, out object rawValue)
        {
            switch (sourceType)
            {
                case JTokenType.Boolean when targetType == PrimitiveType.Boolean:
                    rawValue = (bool)jsonValue;
                    return true;

                case JTokenType.Integer:
                    return TryParseNumericValue(jsonValue, targetType, out rawValue);

                case JTokenType.Float:
                    return TryParseNumericValue(jsonValue, targetType, out rawValue);

                case JTokenType.String:
                    return TryParseStringValue(jsonValue, targetType, out rawValue);

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static bool TryParseNumericValue(JValue jsonValue, PrimitiveType targetType, out object rawValue)
        {
            switch (targetType)
            {
                case PrimitiveType.Byte:
                    rawValue = (byte)jsonValue;
                    return true;

                case PrimitiveType.Int16:
                    rawValue = (short)jsonValue;
                    return true;

                case PrimitiveType.Int32:
                    rawValue = (int)jsonValue;
                    return true;

                case PrimitiveType.Int64:
                    rawValue = (long)jsonValue;
                    return true;

                case PrimitiveType.Float:
                    rawValue = (float)jsonValue;
                    return true;

                case PrimitiveType.Double:
                    rawValue = (double)jsonValue;
                    return true;

                case PrimitiveType.Decimal:
                    rawValue = (decimal)jsonValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static bool TryParseStringValue(JValue jsonValue, PrimitiveType targetType, out object rawValue)
        {
            switch (targetType)
            {
                case PrimitiveType.DateTime:
                    rawValue = (DateTime)jsonValue;
                    return true;

                case PrimitiveType.DateTimeOffset:
                    rawValue = (DateTimeOffset)jsonValue;
                    return true;

                case PrimitiveType.String:
                    rawValue = (string)jsonValue;
                    return true;

                case PrimitiveType.Uri:
                    rawValue = (Uri)jsonValue;
                    return true;

                case PrimitiveType.UUID:
                    rawValue = (Guid)jsonValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static ValueReference ParseEnumValue(JValue jsonValue, JTokenType sourceType, SchemaTypeReference targetType, string filePath, IJsonLineInfo location, ILogger logger)
        {
            switch (sourceType)
            {
                case JTokenType.Integer:
                    int intValue = (int)jsonValue;
                    return new EnumMemberNumericReference(targetType, intValue, filePath, location.LineNumber, location.LinePosition);

                case JTokenType.String:
                    string strValue = (string)jsonValue;
                    return new EnumMemberStringReference(targetType, strValue, filePath, location.LineNumber, location.LinePosition);

                default:
                    logger.LogError($"Unexpected constant value type: {sourceType}", filePath, location.LineNumber, location.LinePosition);
                    return null;
            }
        }
    }
}