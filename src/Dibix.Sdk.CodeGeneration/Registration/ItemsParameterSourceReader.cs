using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ItemsParameterSourceReader(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry) : IParameterSourceReader
    {
        private readonly IParameterSourceReader _propertyPathParameterSourceReader = new PropertyPathParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry);
        private readonly ItemParameterSourceReader _itemParameterSourceReader = new ItemParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);

        ActionParameterSourceBuilder IParameterSourceReader.Read(JToken value, JTokenType type, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            if (type != JTokenType.Object)
                return null;

            JObject @object = (JObject)value;
            JProperty itemsProperty = @object.Property("items");
            if (itemsProperty != null)
                return CollectItemsParameterSource(@object, itemsProperty, requestBody, pathParameters);

            return null;
        }

        private ActionParameterSourceBuilder CollectItemsParameterSource(JObject @object, JProperty itemsProperty, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            JProperty sourceProperty = @object.GetPropertySafe("source");
            ActionParameterSourceBuilder source = _propertyPathParameterSourceReader.Read(sourceProperty.Value, sourceProperty.Value.Type, requestBody, pathParameters, rootParameterSourceBuilder: null);
            if (source is not ActionParameterPropertySourceBuilder propertySourceBuilder)
            {
                throw new InvalidOperationException($@"Unexpected root source for items parameter source
Expected: {typeof(ActionParameterPropertySourceBuilder)}
Actual: {source?.GetType()}");
            }

            JObject itemsObject = (JObject)itemsProperty.Value;
            foreach (JProperty itemProperty in itemsObject.Properties())
            {
                ActionParameterSourceBuilder itemSource = _itemParameterSourceReader.Read(itemProperty, requestBody, pathParameters, propertySourceBuilder);
                SourceLocation sourceInfo = itemProperty.GetSourceInfo();
                propertySourceBuilder.ItemSources.Add(new ActionParameterItemSourceBuilder(itemProperty.Name, sourceBuilder: itemSource, sourceInfo, schemaRegistry, logger));
            }

            return propertySourceBuilder;
        }
    }

    internal sealed class ItemParameterSourceReader(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry)
        : ParameterSourceReaderBase(CollectReaders(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry))
    {
        private static IEnumerable<IParameterSourceReader> CollectReaders(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry)
        {
            yield return new ConstantParameterSourceReader(schemaRegistry, logger);
            yield return new PropertyPathParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry);
            yield return new ParameterSourceWithConverterReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);
        }

        public override ActionParameterSourceBuilder Read(JProperty property, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            ActionParameterSourceBuilder source = base.Read(property, requestBody, pathParameters, rootParameterSourceBuilder);
            return source ?? throw new InvalidOperationException($"Unexpected parameter source format at {property.Path}");
        }
    }
}