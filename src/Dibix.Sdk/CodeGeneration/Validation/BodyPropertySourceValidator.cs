﻿using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BodyPropertySourceValidator : StaticActionParameterPropertySourceValidator<BodyParameterSource>
    {
        public override bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TypeReference type = actionDefinition.RequestBody?.Contract;
            string[] parts = value.PropertyName.Split('.');
            int columnOffset = 0;
            foreach (string propertyName in parts)
            {
                if (!(type is SchemaTypeReference schemaTypeReference) 
                 || !(schemaRegistry.GetSchema(schemaTypeReference) is ObjectSchema objectSchema))
                    continue;

                ObjectSchemaProperty property = objectSchema.Properties.SingleOrDefault(x => x.Name == propertyName);
                if (property != null)
                {
                    type = property.Type;
                    columnOffset += propertyName.Length + 1; // Skip property name + dot
                    continue;
                }

                int definitionNameOffset = value.Definition.Name.Length + 1; // Skip source name + dot
                int column = value.Column + definitionNameOffset + columnOffset;
                logger.LogError(null, $"Property '{propertyName}' not found on contract '{objectSchema.FullName}'", value.FilePath, value.Line, column);
                return false;
            }
            return true;
        }
    }
}