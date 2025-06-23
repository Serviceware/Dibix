using System;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlValueReferenceParser
    {
        public static ValueReference Parse(ScalarExpression value, TypeReference targetType, string filePath, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            switch (value)
            {
                case NullLiteral _:
                    return new NullValueReference(targetType, new SourceLocation(filePath, value.StartLine, value.StartColumn));

                case Literal literal:
                    return ParseDefaultValue(literal, targetType, filePath, schemaRegistry, logger);

                //case VariableReference variableReference:
                //  return this.TryParseDefaultValue(parameterName, variableReference, targetType);

                default:
                    logger.LogError($"Unexpected constant value type: {value?.GetType()})", filePath, value.StartLine, value.StartColumn);
                    return null;
            }
        }
        private static ValueReference ParseDefaultValue(Literal value, TypeReference targetType, string filePath, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            SourceLocation location = new SourceLocation(filePath, value.StartLine, value.StartColumn);
            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    if (TryParseDefaultValue(value, value.LiteralType, primitiveTypeReference, out object rawValue))
                        return new PrimitiveValueReference(primitiveTypeReference, rawValue, location);

                    logger.LogError($"Could not convert value '{value.Dump()}' to type '{primitiveTypeReference.Type}'", location);
                    return null;

                case SchemaTypeReference schemaTypeReference:
                    SchemaDefinition schemaDefinition = schemaRegistry.GetSchema(schemaTypeReference);
                    if (schemaDefinition is EnumSchema enumSchema)
                        return ParseEnumValue(value, value.LiteralType, schemaTypeReference, enumSchema, filePath, logger);

                    logger.LogError($"Unexpected schema type for constant value: {schemaDefinition?.GetType()}", location);
                    return null;

                default:
                    logger.LogError($"Unexpected target type for constant value: {targetType?.GetType()}", location);
                    return null;
            }
        }

        private static bool TryParseDefaultValue(Literal literal, LiteralType sourceType, PrimitiveTypeReference targetType, out object rawValue)
        {
            switch (sourceType)
            {
                case LiteralType.Integer when targetType.Type == PrimitiveType.Boolean:
                    rawValue = literal.Value == "1";
                    return true;

                case LiteralType.Integer:
                    return TryParseNumericValue(literal.Value, targetType.Type, out rawValue);

                case LiteralType.String:
                    return TryParseStringValue(literal.Value, targetType.Type, out rawValue);

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static bool TryParseNumericValue(string value, PrimitiveType targetType, out object rawValue)
        {
            switch (targetType)
            {
                case PrimitiveType.Byte when Byte.TryParse(value, out byte byteValue):
                    rawValue = byteValue;
                    return true;

                case PrimitiveType.Int16 when Int16.TryParse(value, out short int16Value):
                    rawValue = int16Value;
                    return true;

                case PrimitiveType.Int32 when Int32.TryParse(value, out int int32Value):
                    rawValue = int32Value;
                    return true;

                case PrimitiveType.Int64 when Int64.TryParse(value, out long int64Value):
                    rawValue = int64Value;
                    return true;

                case PrimitiveType.Float when Single.TryParse(value, out float singleValue):
                    rawValue = singleValue;
                    return true;

                case PrimitiveType.Double when Double.TryParse(value, out double doubleValue):
                    rawValue = doubleValue;
                    return true;

                case PrimitiveType.Decimal when Decimal.TryParse(value, out decimal decimalValue):
                    rawValue = decimalValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static bool TryParseStringValue(string value, PrimitiveType targetType, out object rawValue)
        {
            switch (targetType)
            {
                case PrimitiveType.Date when DateTime.TryParse(value, out DateTime dateTimeValue):
                    rawValue = dateTimeValue;
                    return true;

                case PrimitiveType.Time when DateTime.TryParse(value, out DateTime dateTimeValue):
                    rawValue = dateTimeValue;
                    return true;

                case PrimitiveType.DateTime when DateTime.TryParse(value, out DateTime dateTimeValue):
                    rawValue = dateTimeValue;
                    return true;

                case PrimitiveType.DateTimeOffset when DateTimeOffset.TryParse(value, out DateTimeOffset dateTimeOffsetValue):
                    rawValue = dateTimeOffsetValue;
                    return true;

                case PrimitiveType.String:
                    rawValue = value;
                    return true;

                case PrimitiveType.Uri when Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri uriValue):
                    rawValue = uriValue;
                    return true;

                case PrimitiveType.UUID when Guid.TryParse(value, out Guid guidValue):
                    rawValue = guidValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static ValueReference ParseEnumValue(Literal literal, LiteralType sourceType, SchemaTypeReference targetType, EnumSchema schema, string filePath, ILogger logger)
        {
            SourceLocation location = new SourceLocation(filePath, literal.StartLine, literal.StartColumn);
            switch (sourceType)
            {
                case LiteralType.Integer when Int32.TryParse(literal.Value, out int intValue):
                {
                    EnumSchemaMember member = schema.Members.SingleOrDefault(x => Equals(x.ActualValue, intValue));
                    if (member == null)
                    {
                        logger.LogError($"Enum '{schema.FullName}' does not define a member with value '{intValue}'", location);
                        return null;
                    }
                    return new EnumMemberReference(targetType, member, EnumMemberReferenceKind.Value, location);
                }

                case LiteralType.String:
                {
                    string strValue = literal.Value;
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