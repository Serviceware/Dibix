using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionParameterFixedPropertySourceValidator<TSource> : ActionParameterPropertySourceValidator<TSource>, IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition, IActionParameterFixedPropertySourceDefinition
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, IActionParameterPropertySource currentValue, IActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (Definition.Properties.Any(x => x.Name == currentValue.PropertyName)) 
                return true;

            int column = currentValue.Location.Column + Definition.Name.Length + 1; // Skip source name + dot
            logger.LogError($"Source '{Definition.Name}' does not support property '{currentValue.PropertyName}'", currentValue.Location.Source, currentValue.Location.Line, column);
            return false;
        }
    }
}