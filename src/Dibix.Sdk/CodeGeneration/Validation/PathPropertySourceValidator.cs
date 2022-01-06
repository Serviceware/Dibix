namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PathPropertySourceValidator : StaticActionParameterPropertySourceValidator<PathParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger) => true;
    }
}