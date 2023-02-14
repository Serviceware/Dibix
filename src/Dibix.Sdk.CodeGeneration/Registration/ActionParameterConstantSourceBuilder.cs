using System;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterConstantSourceBuilder : ActionParameterSourceBuilder
    {
        private readonly JValue _value;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public ActionParameterConstantSourceBuilder(JValue value, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _value = value;
            _schemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public override ActionParameterSource Build(TypeReference type)
        {
            if (type == null) // Information not available => ExternalReflectionActionTargetDefinitionResolver
            {
                SourceLocation location = _value.GetSourceInfo();
                return new ActionParameterConstantSource(BuildConstantValueReference(_value.Type, location));
            }

            ValueReference valueReference = JsonValueReferenceParser.Parse(type, _value, _schemaRegistry, _logger);
            return new ActionParameterConstantSource(valueReference);
        }

        private ValueReference BuildConstantValueReference(JTokenType type, SourceLocation location)
        {
            switch (type)
            {
                case JTokenType.Integer: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false, location), (int)_value, location);
                case JTokenType.String: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, location), (string)_value, location);
                case JTokenType.Boolean: return new PrimitiveValueReference(new PrimitiveTypeReference(PrimitiveType.Boolean, isNullable: false, isEnumerable: false, location), (bool)_value, location);
                case JTokenType.Null: return new NullValueReference(type: null, location);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}