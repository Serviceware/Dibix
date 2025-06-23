using System;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class JsonValueReferenceParser
    {
        public static ValueReference Parse(TypeReference targetType, JValue value, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            SourceLocation sourceInfo = value.GetSourceInfo();

            if (value.Type == JTokenType.Null)
            {
                if (!targetType.IsNullable)
                    logger.LogError($"Non-nullable type '{targetType.DisplayName}' cannot be initialized with a null value", sourceInfo);

                return new NullValueReference(targetType, sourceInfo);
            }

            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    if (TryParseValue(value, value.Type, primitiveTypeReference.Type, out object rawValue))
                        return new PrimitiveValueReference(primitiveTypeReference, rawValue, sourceInfo);

                    logger.LogError($"Could not convert value '{value}' to type '{primitiveTypeReference.Type}'", sourceInfo);
                    return null;

                case SchemaTypeReference schemaTypeReference:
                    SchemaDefinition schemaDefinition = schemaRegistry.GetSchema(schemaTypeReference);
                    if (schemaDefinition is EnumSchema enumSchema)
                        return ParseEnumValue(value, value.Type, schemaTypeReference, enumSchema, sourceInfo, logger);

                    logger.LogError($"Unexpected schema type for constant value: {schemaDefinition?.GetType()}", sourceInfo);
                    return null;

                default:
                    logger.LogError($"Unexpected target type for constant value: {targetType?.GetType()}", sourceInfo);
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
                case PrimitiveType.Date:
                    rawValue = (DateTime)jsonValue;
                    return true;

                case PrimitiveType.Time:
                    rawValue = (DateTime)jsonValue;
                    return true;

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

        private static ValueReference ParseEnumValue(JValue jsonValue, JTokenType sourceType, SchemaTypeReference targetType, EnumSchema schema, SourceLocation location, ILogger logger)
        {
            switch (sourceType)
            {
                case JTokenType.Integer:
                {
                    int intValue = (int)jsonValue;
                    EnumSchemaMember member = schema.Members.SingleOrDefault(x => Equals(x.ActualValue, intValue));
                    if (member == null)
                    {
                        logger.LogError($"Enum '{schema.FullName}' does not define a member with value '{intValue}'", location);
                        return null;
                    }
                    return new EnumMemberReference(targetType, member, EnumMemberReferenceKind.Value, location);
                }

                case JTokenType.String:
                {
                    string strValue = (string)jsonValue;
                    EnumSchemaMember member = schema.Members.SingleOrDefault(x => x.Name == strValue);
                    if (member == null)
                    {
                        logger.LogError($"Enum '{schema.FullName}' does not define a member named '{strValue}'", location);
                        return null;
                    }
                    return new EnumMemberReference(targetType, member, EnumMemberReferenceKind.Name, location);
                }

                default:
                    logger.LogError($"Unexpected constant value type: {sourceType}", location);
                    return null;
            }
        }
    }
}