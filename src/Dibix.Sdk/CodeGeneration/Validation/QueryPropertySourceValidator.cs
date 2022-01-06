namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class QueryPropertySourceValidator : StaticActionParameterPropertySourceValidator<QueryParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger) => true;
    }
}