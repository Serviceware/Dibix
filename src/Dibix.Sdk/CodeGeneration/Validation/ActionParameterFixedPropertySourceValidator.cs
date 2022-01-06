using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionParameterFixedPropertySourceValidator<TSource> : ActionParameterPropertySourceValidator<TSource>, IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition, IActionParameterFixedPropertySourceDefinition
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (Definition.Properties.Contains(value.PropertyName)) 
                return true;

            int column = value.Column + value.Definition.Name.Length + 1; // Skip source name + dot
            logger.LogError(null, $"Source '{value.Definition.Name}' does not support property '{value.PropertyName}'", value.FilePath, value.Line, column);
            return false;
        }
    }
}