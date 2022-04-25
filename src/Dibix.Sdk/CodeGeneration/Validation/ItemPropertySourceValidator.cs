using System;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ItemPropertySourceValidator : ObjectSchemaPropertySourceValidator<ItemParameterSource>
    {
        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (parentValue == null)
                throw new ArgumentNullException(nameof(parentValue), "Item property source must map from a parent property source");

            if (!(rootParameter.Type is SchemaTypeReference parameterSchemaTypeReference))
            {
                logger.LogError(null, $"Unexpected parameter type '{rootParameter.Type}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.FilePath, currentValue.Line, currentValue.Column);
                return false;
            }

            SchemaDefinition parameterSchema = schemaRegistry.GetSchema(parameterSchemaTypeReference);
            if (!(parameterSchema is UserDefinedTypeSchema userDefinedTypeSchema))
            {
                logger.LogError(null, $"Unexpected parameter type '{parameterSchema}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.FilePath, currentValue.Line, currentValue.Column);
                return false;
            }

            if (!userDefinedTypeSchema.Properties.Any(x => String.Equals(x.Name, currentParameter.ParameterName, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError(null, $"Column '{currentParameter.ParameterName}' does not exist on UDT '{userDefinedTypeSchema.UdtName}'", currentParameter.FilePath, currentParameter.Line, currentParameter.Column);
                return false;
            }

            if (parentValue.Definition is QueryParameterSource)
                return true;

            if (!(parentValue.Definition is BodyParameterSource))
            {
                // Mapping from an enumerable item, means that the source property must be enumerable.
                // However mapping from a custom source with an enumerable property is currently not supported.
                logger.LogError(null, $"Source '{parentValue.Definition.Name}' does not support mapping properties to an UDT", parentValue.FilePath, parentValue.Line, parentValue.Column);
                return false;
            }

            if (actionDefinition.RequestBody == null)
            {
                // No body => No validation possible
                // This *should* be a warning though
                return true;
            }

            TypeReference bodyContract = actionDefinition.RequestBody.Contract;
            if (bodyContract == null) // Already logged at 'TypeResolverFacade.ResolveType'
                return false;

            if (!(bodyContract is SchemaTypeReference bodySchemaTypeReference))
            {
                logger.LogError(null, $"Unexpected request body contract '{bodyContract}'. Expected object schema when mapping complex UDT parameter: @{rootParameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Source, bodyContract.Line, bodyContract.Column);
                return false;
            }

            SchemaDefinition bodySchema = schemaRegistry.GetSchema(bodySchemaTypeReference);
            if (!(bodySchema is ObjectSchema bodyObjectSchema))
            {
                logger.LogError(null, $"Unexpected request body contract '{bodySchema}'. Expected object schema when mapping complex UDT parameter: @{rootParameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Source, bodyContract.Line, bodyContract.Column);
                return false;
            }

            if (!TryGetProperty(bodyObjectSchema, parentValue.PropertyName, parentValue, logger, out ObjectSchemaProperty itemsProperty))
                return false;

            if (currentValue.PropertyName == ItemParameterSource.IndexPropertyName)
                return true;

            return base.Validate(currentValue, itemsProperty.Type, schemaRegistry, logger);
        }
    }
}