namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PathPropertySourceValidator : StaticActionParameterPropertySourceValidator<PathParameterSource>
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) => true;
    }
}