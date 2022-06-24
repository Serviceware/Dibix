namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyPropertySourceValidator : StaticActionParameterPropertySourceValidator<BodyParameterSource>
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            // Already validated at Dibix.Sdk.CodeGeneration.ControllerDefinitionProvider.CollectBodyPropertySourceNodes
            return true;
        }
    }
}