using System;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CodeGenerationUserConfigurationReader : IUserConfigurationReader
    {
        private readonly CodeGenerationConfiguration _configuration;
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly IActionParameterConverterRegistry _actionParameterConverterRegistry;
        private readonly ILogger _logger;

        public CodeGenerationUserConfigurationReader(CodeGenerationConfiguration configuration, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger)
        {
            _configuration = configuration;
            _actionParameterSourceRegistry = actionParameterSourceRegistry;
            _actionParameterConverterRegistry = actionParameterConverterRegistry;
            _logger = logger;
        }

        public void Read(string filePath, JObject json)
        {
            const string endpointConfigurationName = "Endpoints";
            JObject endpointConfiguration = (JObject)json.Property(endpointConfigurationName)?.Value;
            if (endpointConfiguration == null)
                return;

            JProperty baseUrlProperty = endpointConfiguration.Property(nameof(CodeGenerationConfiguration.BaseUrl));
            if (baseUrlProperty != null)
            {
                _configuration.BaseUrl = (string)baseUrlProperty.Value;
            }

            const string parameterSourcesName = "ParameterSources";
            JObject parameterSources = (JObject)endpointConfiguration.Property(parameterSourcesName)?.Value;
            if (parameterSources != null)
            {
                foreach (JProperty parameterSource in parameterSources.Properties())
                {
                    if (this._actionParameterSourceRegistry.TryGetDefinition(parameterSource.Name, out ActionParameterSourceDefinition _))
                    {
                        IJsonLineInfo lineInfo = parameterSource.GetLineInfo();
                        this._logger.LogError($"Parameter source '{parameterSource.Name}' is already registered", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                        continue;
                    }

                    CollectParameterSource(parameterSource.Name, parameterSource.Value.Type, parameterSource.Value, this._actionParameterSourceRegistry);
                }
            }

            const string convertersName = "Converters";
            JArray converters = (JArray)endpointConfiguration.Property(convertersName)?.Value;
            if (converters != null)
            {
                foreach (JToken converter in converters)
                {
                    string converterName = (string)converter;
                    if (this._actionParameterConverterRegistry.IsRegistered(converterName))
                    {
                        IJsonLineInfo lineInfo = converter.GetLineInfo();
                        this._logger.LogError($"Parameter converter '{converterName}' is already registered", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                        continue;
                    }

                    this._actionParameterConverterRegistry.Register(converterName);
                }
            }
        }

        private static void CollectParameterSource(string sourceName, JTokenType valueType, JToken value, IActionParameterSourceRegistry registry)
        {
            switch (valueType)
            {
                case JTokenType.Null:
                    registry.Register(new DynamicParameterSource(sourceName), x => new DynamicParameterSourceValidator(x));
                    break;

                case JTokenType.Array:
                    JArray parameterSourceProperties = (JArray)value;
                    string[] propertyNames = parameterSourceProperties.Select(x => (string)x).ToArray();
                    registry.Register(new DynamicPropertyParameterSource(sourceName, propertyNames), x => new DynamicPropertyParameterSourceValidator(x));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
            }
        }
    }
}