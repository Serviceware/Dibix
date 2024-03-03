using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class HeaderPropertySourceValidator : StaticActionParameterPropertySourceValidator<HeaderParameterSource>
    {
        public HeaderPropertySourceValidator(HeaderParameterSource definition) : base(definition) { }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger) => true;
    }
}