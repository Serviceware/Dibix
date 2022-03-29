using System;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ItemPropertySourceValidator : ObjectSchemaPropertySourceValidator<ItemParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Item property source must map from a parent property source");

            if (!(parent.Definition is BodyParameterSource))
            {
                // Mapping from an enumerable item, means that the source property must be enumerable.
                // However mapping from a custom source with an enumerable property is currently not supported.
                throw new InvalidOperationException("Complex/enumerable source properties are currently not supported");
            }

            TypeReference bodyTypeReference = actionDefinition.RequestBody?.Contract; 
            if (bodyTypeReference == null)
            {
                // No body contract => No validation possible
                // This *should* be a warning though
                return true;
            }

            if (!(bodyTypeReference is SchemaTypeReference bodySchemaTypeReference))
                throw new InvalidOperationException($"Unexpected body contract type '{bodyTypeReference}'. Must be a complex object contract.");

            SchemaDefinition bodySchema = schemaRegistry.GetSchema(bodySchemaTypeReference);
            if (bodySchema == null)
                return false;
            
            if (!(bodySchema is ObjectSchema bodyObjectSchema))
                throw new InvalidOperationException($"Unexpected body contract type '{bodySchema} ({bodySchema.FullName})'. Must be a complex object contract.");

            ObjectSchemaProperty itemsProperty = bodyObjectSchema.Properties.SingleOrDefault(x => x.Name == parent.PropertyName);
            if (itemsProperty == null)
                throw new InvalidOperationException($"Could not find a property '{parent.PropertyName}' on contract '{bodyObjectSchema.FullName}'");

            if (value.PropertyName == ItemParameterSource.IndexPropertyName)
                return true;

            return base.Validate(value, itemsProperty.Type, schemaRegistry, logger);
        }
    }
}