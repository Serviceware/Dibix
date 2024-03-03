using System;
using System.Linq;
using Dibix.Sdk.Abstractions;
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

        public void Read(JObject json)
        {
            const string endpointConfigurationName = "Endpoints";
            JObject endpointConfiguration = (JObject)json.Property(endpointConfigurationName)?.Value;
            if (endpointConfiguration == null)
                return;

            CollectBaseUrl(endpointConfiguration);
            CollectParameterSources(endpointConfiguration);
            CollectConverters(endpointConfiguration);
            CollectCustomSecuritySchemes(endpointConfiguration);
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

        private void CollectParameterSources(JObject endpointConfiguration)
        {
            const string propertyName = "ParameterSources";
            JObject parameterSources = (JObject)endpointConfiguration.Property(propertyName)?.Value;
            if (parameterSources == null) 
                return;

            foreach (JProperty parameterSource in parameterSources.Properties())
            {
                IActionParameterExtensibleFixedPropertySourceDefinition extensiblePropertyDefinition = null;
                if (_actionParameterSourceRegistry.TryGetDefinition(parameterSource.Name, out ActionParameterSourceDefinition sourceDefinition))
                {
                    switch (sourceDefinition)
                    {
                        case ClaimParameterSource claimParameterSource:
                            CollectClaimMapping((JObject)parameterSource.Value, claimParameterSource);
                            return;
                        
                        case IActionParameterExtensibleFixedPropertySourceDefinition extensiblePropertySourceDefinition:
                            extensiblePropertyDefinition = extensiblePropertySourceDefinition;
                            break;

                        default:
                            SourceLocation sourceInfo = parameterSource.GetSourceInfo();
                            _logger.LogError($"Parameter source '{parameterSource.Name}' is already registered", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                            continue;
                    }
                }

                CollectParameterSource(parameterSource.Name, parameterSource.Value.Type, parameterSource.Value, _actionParameterSourceRegistry, extensiblePropertyDefinition);
            }
        }

        private static void CollectParameterSource(string sourceName, JTokenType valueType, JToken value, IActionParameterSourceRegistry registry, IActionParameterExtensibleFixedPropertySourceDefinition extensiblePropertyDefinition)
        {
            switch (valueType)
            {
                case JTokenType.Null:
                    registry.Register(new DynamicParameterSource(sourceName), x => new DynamicParameterSourceValidator(x));
                    break;

                case JTokenType.Array:
                    JArray parameterSourceProperties = (JArray)value;
                    string[] propertyNames = parameterSourceProperties.Select(x => (string)x).ToArray();
                    if (extensiblePropertyDefinition != null)
                    {
                        extensiblePropertyDefinition.AddProperties(propertyNames);
                    }
                    else
                    {
                        registry.Register(new DynamicPropertyParameterSource(sourceName, propertyNames), x => new DynamicPropertyParameterSourceValidator(x));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
            }
        }

        private void CollectClaimMapping(JObject claimMappings, ClaimParameterSource claimParameterSource)
        {
            foreach (JProperty claimMapping in claimMappings.Properties())
            {
                string claimPropertyName = claimMapping.Name;
                SourceLocation sourceInfo = claimMapping.GetSourceInfo();
                if (claimParameterSource.TryGetClaimTypeName(claimPropertyName, out string existingClaimTypeName))
                {
                    _logger.LogError($"Claim property '{claimPropertyName}' is already registered using claim type '{existingClaimTypeName}'", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                    continue;
                }

                string claimTypeName = (string)claimMapping.Value;
                if (claimParameterSource.TryGetPropertyName(claimTypeName, out string existingClaimPropertyName))
                {
                    _logger.LogError($"Claim type '{existingClaimPropertyName}' is already registered using property '{existingClaimPropertyName}'", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                    continue;
                }

                claimParameterSource.Register(claimPropertyName, claimTypeName);
            }
        }

        private void CollectConverters(JObject endpointConfiguration)
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
                    SourceLocation sourceInfo = converter.GetSourceInfo();
                    _logger.LogError($"Parameter converter '{converterName}' is already registered", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                    continue;
                }

                _actionParameterConverterRegistry.Register(converterName);
            }
        }

        private void CollectCustomSecuritySchemes(JObject endpointConfiguration)
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

                SourceLocation sourceInfo = customSecurityScheme.GetSourceInfo();
                _logger.LogError($"Security scheme '{customSecuritySchemeName}' is already registered", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
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