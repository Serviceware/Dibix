namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterPropertySourceValidator
    {
        bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger);
    }
}