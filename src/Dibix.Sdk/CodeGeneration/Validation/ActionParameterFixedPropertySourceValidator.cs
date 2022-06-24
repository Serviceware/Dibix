using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionParameterFixedPropertySourceValidator<TSource> : ActionParameterPropertySourceValidator<TSource>, IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition, IActionParameterFixedPropertySourceDefinition
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            if (Definition.Properties.Contains(currentValue.PropertyName)) 
                return true;

            int column = currentValue.Column + currentValue.Definition.Name.Length + 1; // Skip source name + dot
            logger.LogError($"Source '{currentValue.Definition.Name}' does not support property '{currentValue.PropertyName}'", currentValue.FilePath, currentValue.Line, column);
            return false;
        }
    }
}