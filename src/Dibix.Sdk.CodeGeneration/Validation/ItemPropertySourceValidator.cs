﻿using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ItemPropertySourceValidator : StaticActionParameterPropertySourceValidator<ItemParameterSource>
    {
        public ItemPropertySourceValidator(ItemParameterSource definition) : base(definition) { }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, IActionParameterPropertySource currentValue, IActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (parentValue == null)
                throw new ArgumentNullException(nameof(parentValue), "Item property source must map from a parent property source");

            if (!(rootParameter.Type is SchemaTypeReference parameterSchemaTypeReference))
            {
                logger.LogError($"Unexpected parameter type '{rootParameter.Type?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.Location.Source, currentValue.Location.Line, currentValue.Location.Column);
                return false;
            }

            SchemaDefinition parameterSchema = schemaRegistry.GetSchema(parameterSchemaTypeReference);
            if (!(parameterSchema is UserDefinedTypeSchema))
            {
                logger.LogError($"Unexpected parameter type '{parameterSchema?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", currentValue.Location.Source, currentValue.Location.Line, currentValue.Location.Column);
                return false;
            }

            // Already validated at Dibix.Sdk.CodeGeneration.ControllerDefinitionProvider.CollectItemPropertySourceNodes
            return true;
        }
    }
}