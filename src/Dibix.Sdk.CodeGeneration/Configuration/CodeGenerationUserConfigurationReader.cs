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
        private readonly SecuritySchemes _securitySchemes;
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly IActionParameterConverterRegistry _actionParameterConverterRegistry;
        private readonly ILogger _logger;

        public CodeGenerationUserConfigurationReader(CodeGenerationConfiguration configuration, SecuritySchemes securitySchemes, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger)
        {
            _configuration = configuration;
            _securitySchemes = securitySchemes;
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

            CollectBaseUrl(endpointConfiguration);
            CollectParameterSources(filePath, endpointConfiguration);
            CollectConverters(filePath, endpointConfiguration);
            CollectCustomSecuritySchemes(filePath, endpointConfiguration);
            CollectTemplates(endpointConfiguration);
        }

        private void CollectBaseUrl(JObject endpointConfiguration)
        {
            JProperty baseUrlProperty = endpointConfiguration.Property(nameof(CodeGenerationConfiguration.BaseUrl));
            if (baseUrlProperty != null)
            {
                _configuration.BaseUrl = (string)baseUrlProperty.Value;
            }
        }

        private void CollectParameterSources(string filePath, JObject endpointConfiguration)
        {
            const string propertyName = "ParameterSources";
            JObject parameterSources = (JObject)endpointConfiguration.Property(propertyName)?.Value;
            if (parameterSources == null) 
                return;

            foreach (JProperty parameterSource in parameterSources.Properties())
            {
                if (_actionParameterSourceRegistry.TryGetDefinition(parameterSource.Name, out ActionParameterSourceDefinition _))
                {
                    IJsonLineInfo lineInfo = parameterSource.GetLineInfo();
                    _logger.LogError($"Parameter source '{parameterSource.Name}' is already registered", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                    continue;
                }

                CollectParameterSource(parameterSource.Name, parameterSource.Value.Type, parameterSource.Value, _actionParameterSourceRegistry);
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

        private void CollectConverters(string filePath, JObject endpointConfiguration)
        {
            const string propertyName = "Converters";
            JArray converters = (JArray)endpointConfiguration.Property(propertyName)?.Value;
            if (converters == null) 
                return;

            foreach (JToken converter in converters)
            {
                string converterName = (string)converter;
                if (_actionParameterConverterRegistry.IsRegistered(converterName))
                {
                    IJsonLineInfo lineInfo = converter.GetLineInfo();
                    _logger.LogError($"Parameter converter '{converterName}' is already registered", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                    continue;
                }

                _actionParameterConverterRegistry.Register(converterName);
            }
        }

        private void CollectCustomSecuritySchemes(string filePath, JObject endpointConfiguration)
        {
            const string propertyName = "CustomSecuritySchemes";
            JArray customSecuritySchemes = (JArray)endpointConfiguration.Property(propertyName)?.Value;
            if (customSecuritySchemes == null) 
                return;

            foreach (JToken customSecurityScheme in customSecuritySchemes)
            {
                string customSecuritySchemeName = (string)customSecurityScheme;
                if (_securitySchemes.RegisterSecurityScheme(new SecurityScheme(customSecuritySchemeName, SecuritySchemeKind.ApiKey))) 
                    continue;

                IJsonLineInfo lineInfo = customSecurityScheme.GetLineInfo();
                _logger.LogError($"Security scheme '{customSecuritySchemeName}' is already registered", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
            }
        }

        private void CollectTemplates(JObject endpointConfiguration)
        {
            const string propertyName = "Templates";
            JObject templates = (JObject)endpointConfiguration.Property(propertyName)?.Value;
            if (templates == null) 
                return;

            foreach (JProperty template in templates.Properties())
            {
                if (template.Name == "Authorization")
                    CollectAuthorizationTemplate((JObject)template.Value);
                else
                    CollectTemplate(template.Name, (JObject)template.Value);
            }
        }

        private void CollectTemplate(string name, JObject templateJson)
        {
            ConfigurationTemplate template = new ConfigurationTemplate(name);
            CollectTemplate(template, templateJson);
            _configuration.ConfigurationTemplates.Register(template);
        }
        private static void CollectTemplate(ConfigurationTemplate template, JObject templateJson)
        {
            const string propertyName = "Action";
            JProperty actionTemplateProperty = templateJson.Property(propertyName)!;
            template.Action = (JObject)actionTemplateProperty.Value;
        }

        private void CollectAuthorizationTemplate(JObject templates)
        {
            foreach (JProperty templateJson in templates.Properties())
            {
                ConfigurationAuthorizationTemplate template = new ConfigurationAuthorizationTemplate(templateJson.Name, (JObject)templateJson.Value);
                _configuration.ConfigurationTemplates.Authorization.Register(template);
            }
        }
    }
}