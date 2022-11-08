using System;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterConstantSourceBuilder : ActionParameterSourceBuilder
    {
        private readonly JValue _value;
        private readonly string _filePath;
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;

        public ActionParameterConstantSourceBuilder(JValue value, string filePath, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            _value = value;
            _filePath = filePath;
            _schemaDefinitionResolver = schemaDefinitionResolver;
            _logger = logger;
        }

        public override ActionParameterSource Build(TypeReference type)
        {
            if (type == null) // Information not available => ExternalReflectionActionTargetDefinitionResolver
            {
                IJsonLineInfo location = _value.GetLineInfo();
                return new ActionParameterConstantSource(BuildConstantValueReference(_value.Type, location));
            }

            ValueReference valueReference = JsonValueReferenceParser.Parse(type, _value, _filePath, _schemaDefinitionResolver, _logger);
            return new ActionParameterConstantSource(valueReference);
        }

        private ValueReference BuildConstantValueReference(JTokenType type, IJsonLineInfo location)
        {
            switch (type)
            {
                case JTokenType.Integer: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false, _filePath, location.LineNumber, location.LinePosition), (int)_value, _filePath, location.LineNumber, location.LinePosition);
                case JTokenType.String: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, _filePath, location.LineNumber, location.LinePosition), (string)_value, _filePath, location.LineNumber, location.LinePosition);
                case JTokenType.Boolean: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.Boolean, isNullable: false, isEnumerable: false, _filePath, location.LineNumber, location.LinePosition), (bool)_value, _filePath, location.LineNumber, location.LinePosition);
                case JTokenType.Null: return new NullValueReference(type: null, _filePath, location.LineNumber, location.LinePosition);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}