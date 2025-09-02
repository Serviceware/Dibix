using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ParameterSourceDescriptorReader(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry) : IParameterSourceReader
    {
        private readonly RootParameterSourceReader _parameterSourceReader = new RootParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);

        ActionParameterSourceBuilder IParameterSourceReader.Read(JToken value, JTokenType type, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            if (type != JTokenType.Object)
                return null;

            JObject @object = (JObject)value;
            JProperty sourceProperty = @object.Property("source");
            JProperty converterProperty = @object.Property("converter");
            if (sourceProperty == null)
                return null;

            ActionParameterSourceBuilder source = _parameterSourceReader.Read(sourceProperty, requestBody, pathParameters, rootParameterSourceBuilder);

            if (converterProperty != null)
            {
                if (source is not ActionParameterPropertySourceBuilder propertySourceBuilder)
                {
                    throw new InvalidOperationException($@"Unexpected source for parameter converter
Expected: {typeof(ActionParameterPropertySourceBuilder)}
Actual: {source?.GetType()}");
                }

                JToken converter = converterProperty.Value;
                string converterName = (string)converter;
                if (!actionParameterConverterRegistry.IsRegistered(converterName))
                {
                    SourceLocation converterLocation = converter.GetSourceInfo();
                    logger.LogError($"Unknown property converter '{converterName}'", converterLocation.Source, converterLocation.Line, converterLocation.Column);
                }

                propertySourceBuilder.Converter = converterName;
            }

            return source;
        }
    }
}