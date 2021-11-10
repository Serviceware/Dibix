using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefaultValueCollectionAction : IDelayedContractCollectionAction
    {
        private readonly ObjectSchemaProperty _property;
        private readonly JValue _defaultValueJson;
        private readonly string _filePath;
        private readonly IDictionary<string, ContractDefinition> _contractMap;
        private readonly ILogger _logger;

        public ContractDefaultValueCollectionAction(ObjectSchemaProperty property, JValue defaultValueJson, string filePath, IDictionary<string, ContractDefinition> contractMap, ILogger logger)
        {
            this._property = property;
            this._defaultValueJson = defaultValueJson;
            this._filePath = filePath;
            this._logger = logger;
            this._contractMap = contractMap;
        }

        void IDelayedContractCollectionAction.Invoke()
        {
            IJsonLineInfo defaultValueLocation = this._defaultValueJson.GetLineInfo();
            if (this.TryParseDefaultValue(this._property.Type, this._property.Name, this._defaultValueJson, this._filePath, defaultValueLocation, out object defaultValueRaw))
                this._property.DefaultValue = new DefaultValue(defaultValueRaw, this._filePath, defaultValueLocation.LineNumber, defaultValueLocation.LinePosition);
        }

        private bool TryParseDefaultValue(TypeReference typeReference, string propertyName, JValue jsonValue, string filePath, IJsonLineInfo location, out object rawValue)
        {
            switch (typeReference)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                {
                    if (TryParseDefaultValue(jsonValue, jsonValue.Type, primitiveTypeReference.Type, out rawValue))
                        return true;

                    this._logger.LogError(null, $"Could not convert default value '{propertyName}' to type '{primitiveTypeReference.Type}'", filePath, location.LineNumber, location.LinePosition);
                    return false;
                }

                case SchemaTypeReference schemaTypeReference when this._contractMap[schemaTypeReference.Key].Schema is EnumSchema enumSchema:
                {
                    return this.TryParseEnumValue(jsonValue, jsonValue.Type, enumSchema, filePath, location, out rawValue);
                }

                default:
                {
                    this._logger.LogError(null, $"Default values are only supported for primitive and enum types: {propertyName}", filePath, location.LineNumber, location.LinePosition);
                    rawValue = null;
                    return false;
                }
            }
        }
        private static bool TryParseDefaultValue(JValue jsonValue, JTokenType sourceType, PrimitiveType targetType, out object rawValue)
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

                // Not supported by OpenAPI
                //case PrimitiveType.Decimal:
                //    rawValue = (decimal)jsonValue;
                //    return true;

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

                case PrimitiveType.UUID:
                    rawValue = (Guid)jsonValue;
                    return true;

                default:
                    rawValue = null;
                    return false;
            }
        }

        private bool TryParseEnumValue(JValue jsonValue, JTokenType type, EnumSchema enumSchema, string filePath, IJsonLineInfo location, out object rawValue)
        {
            switch (type)
            {
                case JTokenType.Integer:
                {
                    int intValue = (int)jsonValue;
                    if (enumSchema.TryGetEnumMember(intValue, filePath, location.LineNumber, location.LinePosition, this._logger, out EnumSchemaMember enumMember))
                    {
                        rawValue = enumMember;
                        return true;
                    }
                    rawValue = null;
                    return false;
                }

                case JTokenType.String:
                {
                    string strValue = (string)jsonValue;
                    EnumSchemaMember enumMember = enumSchema.Members.SingleOrDefault(x => x.Name == strValue);
                    if (enumMember != null)
                    {
                        rawValue = enumMember;
                        return true;
                    }
                    this._logger.LogError(code: null, $"Enum '{enumSchema.FullName}' does not define a member named '{strValue}'", filePath, location.LineNumber, location.LinePosition);
                    rawValue = null;
                    return false;
                }

                default:
                    rawValue = null;
                    return false;
            }
        }
    }
}