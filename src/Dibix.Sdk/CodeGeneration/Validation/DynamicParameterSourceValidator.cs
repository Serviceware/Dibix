namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DynamicParameterSourceValidator : ActionParameterPropertySourceValidator<DynamicParameterSource>, IActionParameterPropertySourceValidator
    {
        protected override DynamicParameterSource Definition { get; }

        public DynamicParameterSourceValidator(DynamicParameterSource definition)
        {
            this.Definition = definition;
        }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) => true;
    }
}