using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyPropertySourceValidator : StaticActionParameterPropertySourceValidator<BodyParameterSource>
    {
        public BodyPropertySourceValidator(BodyParameterSource definition) : base(definition) { }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, IActionParameterPropertySource currentValue, IActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            // Already validated at Dibix.Sdk.CodeGeneration.ControllerDefinitionProvider.CollectBodyPropertySourceNodes
            return true;
        }
    }
}