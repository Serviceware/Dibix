namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyPropertySourceValidator : ObjectSchemaPropertySourceValidator<BodyParameterSource>
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (actionDefinition.RequestBody == null)
            {
                // No body => No validation possible
                // This *should* be a warning though
                return true;
            }

            TypeReference bodyContract = actionDefinition.RequestBody.Contract;
            if (bodyContract == null) // Already logged at 'TypeResolverFacade.ResolveType'
                return false;

            return base.Validate(currentValue, bodyContract, schemaRegistry, logger);
        }
    }
}