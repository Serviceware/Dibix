namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class HeaderPropertySourceValidator : StaticActionParameterPropertySourceValidator<HeaderParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger) => true;
    }
}