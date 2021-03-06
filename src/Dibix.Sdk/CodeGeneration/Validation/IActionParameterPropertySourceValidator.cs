namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterPropertySourceValidator
    {
        bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger);
    }
}