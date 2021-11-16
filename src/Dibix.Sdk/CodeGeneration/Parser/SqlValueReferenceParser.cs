using System;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlValueReferenceParser
    {
        public static ValueReference Parse(string parameterName, ScalarExpression value, TypeReference targetType, string filePath, ILogger logger)
        {
            switch (value)
            {
                case NullLiteral _:
                    return new NullValueReference(targetType, filePath, value.StartLine, value.StartColumn);

                case Literal literal:
                    return ParseDefaultValue(parameterName, literal, targetType, filePath, logger);

                //case VariableReference variableReference:
                //  return this.TryParseDefaultValue(parameterName, variableReference, targetType);

                default:
                    logger.LogError(null, $"Unexpected constant value type: {value?.GetType()})", filePath, value.StartLine, value.StartColumn);
                    return null;
            }
        }
        private static ValueReference ParseDefaultValue(string parameterName, Literal value, TypeReference targetType, string filePath, ILogger logger)
        {
            switch (targetType)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    if (TryParseDefaultValue(value, value.LiteralType, primitiveTypeReference, out object rawValue))
                        return new PrimitiveValueReference(primitiveTypeReference, rawValue, filePath, value.StartLine, value.StartColumn);

                    logger.LogError(null, $"Could not convert value '{value.Dump()}' to type '{primitiveTypeReference.Type}'", filePath, value.StartLine, value.StartColumn);
                    return null;

                case SchemaTypeReference schemaTypeReference:
                    return ParseEnumValue(value, value.LiteralType, schemaTypeReference, filePath, logger);

                default:
                    logger.LogError(null, $"Unexpected target type for constant value: {targetType?.GetType()}", filePath, value.StartLine, value.StartColumn);
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
                case PrimitiveType.DateTime when DateTime.TryParse(value, out DateTime dateTimeValue):
                    rawValue = dateTimeValue;
                    return true;

                case PrimitiveType.DateTimeOffset when DateTimeOffset.TryParse(value, out DateTimeOffset dateTimeOffsetValue):
                    rawValue = dateTimeOffsetValue;
                    return true;

                case PrimitiveType.String:
                    rawValue = value;
                    return true;

                case PrimitiveType.UUID when Guid.TryParse(value, out Guid guidValue):
                    rawValue = guidValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private static ValueReference ParseEnumValue(Literal literal, LiteralType sourceType, SchemaTypeReference targetType, string filePath, ILogger logger)
        {
            switch (sourceType)
            {
                case LiteralType.Integer when Int32.TryParse(literal.Value, out int intValue):
                    return new EnumMemberNumericReference(targetType, intValue, filePath, literal.StartLine, literal.StartColumn);

                case LiteralType.String:
                    return new EnumMemberStringReference(targetType, literal.Value, filePath, literal.StartLine, literal.StartColumn);

                default:
                    logger.LogError(null, $"Unexpected constant value type: {sourceType}", filePath, literal.StartLine, literal.StartColumn);
                    return null;
            }
        }
    }
}