using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ItemPropertySourceValidator : StaticActionParameterPropertySourceValidator<ItemParameterSource>
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            if (parentValue == null)
                throw new ArgumentNullException(nameof(parentValue), "Item property source must map from a parent property source");

            if (!(rootParameter.Type is SchemaTypeReference parameterSchemaTypeReference))
            {
                logger.LogError($"Unexpected parameter type '{rootParameter.Type?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.FilePath, currentValue.Line, currentValue.Column);
                return false;
            }

            SchemaDefinition parameterSchema = schemaDefinitionResolver.Resolve(parameterSchemaTypeReference);
            if (!(parameterSchema is UserDefinedTypeSchema))
            {
                logger.LogError($"Unexpected parameter type '{parameterSchema?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.FilePath, currentValue.Line, currentValue.Column);
                return false;
            }

            // Already validated at Dibix.Sdk.CodeGeneration.ControllerDefinitionProvider.CollectItemPropertySourceNodes
            return true;
        }
    }
}