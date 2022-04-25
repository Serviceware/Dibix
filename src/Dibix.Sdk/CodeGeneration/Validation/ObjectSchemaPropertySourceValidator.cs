using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ObjectSchemaPropertySourceValidator<TSource> : StaticActionParameterPropertySourceValidator<TSource> where TSource : ActionParameterSourceDefinition<TSource>, new()
    {
        public abstract override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger);

        protected bool Validate(ActionParameterPropertySource value, TypeReference type, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string[] parts = value.PropertyName.Split('.');
            int columnOffset = 0;
            foreach (string propertyName in parts)
            {
                if (!(type is SchemaTypeReference schemaTypeReference)
                 || !(schemaRegistry.GetSchema(schemaTypeReference) is ObjectSchema objectSchema))
                    continue;

                if (!TryGetProperty(objectSchema, propertyName, value, logger, columnOffset, out ObjectSchemaProperty property)) 
                    return false;

                type = property.Type;
                columnOffset += propertyName.Length + 1; // Skip property name + dot
            }

            return true;
        }

        protected static bool TryGetProperty(ObjectSchema schema, string propertyName, ActionParameterPropertySource source, ILogger logger, out ObjectSchemaProperty property) => TryGetProperty(schema, propertyName, source, logger, columnOffset: 0, out property);
        private static bool TryGetProperty(ObjectSchema schema, string propertyName, ActionParameterPropertySource source, ILogger logger, int columnOffset, out ObjectSchemaProperty property)
        {
            property = schema.Properties.SingleOrDefault(x => x.Name == propertyName);
            if (property != null)
            {
                return true;
            }

            int definitionNameOffset = source.Definition.Name.Length + 1; // Skip source name + dot
            int column = source.Column + definitionNameOffset + columnOffset;
            logger.LogError(null, $"Property '{propertyName}' not found on contract '{schema.FullName}'", source.FilePath, source.Line, column);
            return false;
        }
    }
}