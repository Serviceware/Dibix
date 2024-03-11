using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DynamicParameterSourceValidator : ActionParameterPropertySourceValidator<DynamicParameterSource>, IActionParameterPropertySourceValidator
    {
        protected override DynamicParameterSource Definition { get; }

        public DynamicParameterSourceValidator(DynamicParameterSource definition)
        {
            this.Definition = definition;
        }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, IActionParameterPropertySource currentValue, IActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger) => true;
    }
}