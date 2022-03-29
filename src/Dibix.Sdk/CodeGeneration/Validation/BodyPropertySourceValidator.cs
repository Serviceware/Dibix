namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyPropertySourceValidator : ObjectSchemaPropertySourceValidator<BodyParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TypeReference type = actionDefinition.RequestBody?.Contract;
            if (type == null)
            {
                // No body contract => No validation possible
                // This *should* be a warning though
                return true;
            }

            return base.Validate(value, type, schemaRegistry, logger);
        }
    }
}