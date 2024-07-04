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

                CollectParameterSource(parameterSource.Name, parameterSource.Value.Type, parameterSource.Value, extensiblePropertyDefinition);
            }
        }

        private void CollectParameterSource(string sourceName, JTokenType valueType, JToken value, IActionParameterExtensibleFixedPropertySourceDefinition extensiblePropertyDefinition)
        {
            switch (valueType)
            {
                case JTokenType.Null:
                    _actionParameterSourceRegistry.Register(new DynamicParameterSource(sourceName), x => new DynamicParameterSourceValidator(x));
                    break;

                case JTokenType.Object:
                    JObject @object = (JObject)value;
                    PropertyParameterSourceDescriptor[] propertyNames = @object.Properties()
                                                                               .Select(x => new PropertyParameterSourceDescriptor(x.Name, ResolveParameterSourceType((JValue)x.Value)))
                                                                               .ToArray();

                    if (extensiblePropertyDefinition != null)
                    {
                        extensiblePropertyDefinition.AddProperties(propertyNames);
                    }
                    else
                    {
                        _actionParameterSourceRegistry.Register(new DynamicPropertyParameterSource(sourceName, propertyNames), x => new DynamicPropertyParameterSourceValidator(x));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
            }
        }

        private static PrimitiveTypeReference ResolveParameterSourceType(JValue typeNameValue) => ResolveParameterSourceType(typeNameValue, (string)typeNameValue);
        private static PrimitiveTypeReference ResolveParameterSourceType(JValue typeNameValue, string typeName)
        {
            Guard.IsNotNull(typeNameValue, nameof(typeNameValue));
            SourceLocation typeNameLocation = typeNameValue.GetSourceInfo();
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            NullableTypeName normalizedTypeName = typeName.TrimEnd('*');
            PrimitiveType primitiveType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), normalizedTypeName.Name, ignoreCase: true);
            PrimitiveTypeReference type = new PrimitiveTypeReference(primitiveType, normalizedTypeName.IsNullable, isEnumerable, size: null, typeNameLocation);
            return type;
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

                CollectClaimMapping(claimMapping, claimMapping.Value, claimMapping.Value.Type, claimParameterSource);
            }
        }
        private void CollectClaimMapping(JProperty claimProperty, JToken value, JTokenType type, ClaimParameterSource claimParameterSource)
        {
            switch (type)
            {
                case JTokenType.Object:
                    CollectClaimMappingFromObject(claimProperty.Name, (JObject)value, claimParameterSource);
                    break;

                case JTokenType.String:
                    CollectClaimMappingFromString(claimProperty, claimParameterSource);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        private void CollectClaimMapping(ClaimParameterSource claimParameterSource, string claimPropertyName, string claimTypeName, SourceLocation claimTypeNameLocation, JProperty typeProperty)
        {
            if (claimParameterSource.TryGetPropertyName(claimTypeName, out string existingClaimPropertyName))
            {
                _logger.LogError($"Claim type '{existingClaimPropertyName}' is already registered using property '{existingClaimPropertyName}'", claimTypeNameLocation);
                return;
            }

            PrimitiveTypeReference type = ((JValue)typeProperty.Value).ResolveType(new PrimitiveTypeResolver());
            PropertyParameterSourceDescriptor propertyDescriptor = new PropertyParameterSourceDescriptor(claimPropertyName, type);
            claimParameterSource.Register(propertyDescriptor, claimTypeName);
        }

        private void CollectClaimMappingFromObject(string claimPropertyName, JObject value, ClaimParameterSource claimParameterSource)
        {
            JProperty typeProperty = value.GetPropertySafe("type");
            JProperty claimNameProperty = value.GetPropertySafe("claimName");
            CollectClaimMapping(claimParameterSource, claimPropertyName, claimTypeName: (string)claimNameProperty.Value, claimTypeNameLocation: claimNameProperty.Value.GetSourceInfo(), typeProperty);
        }

        private void CollectClaimMappingFromString(JProperty claimProperty, ClaimParameterSource claimParameterSource)
        {
            string claimPropertyName = claimProperty.Name;
            CollectClaimMapping(claimParameterSource, claimPropertyName, claimTypeName: claimPropertyName, claimTypeNameLocation: claimProperty.GetSourceInfo(), claimProperty);
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
            JObject customSecuritySchemes = (JObject)endpointConfiguration.Property(propertyName)?.Value;
            if (customSecuritySchemes == null) 
                return;

            foreach (JProperty customSecurityScheme in customSecuritySchemes.Properties())
            {
                string schemeName = customSecurityScheme.Name;
                if (CollectCustomSecurityScheme(schemeName, customSecurityScheme.Value, customSecurityScheme.Value.Type)) 
                    continue;

                SourceLocation sourceInfo = customSecurityScheme.GetSourceInfo();
                _logger.LogError($"Security scheme '{schemeName}' is already registered", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
            }
        }

        private bool CollectCustomSecurityScheme(string schemeName, JToken value, JTokenType valueKind)
        {
            switch (valueKind)
            {
                case JTokenType.Object:
                    JObject jObject = (JObject)value;
                    JToken headerName = jObject.GetPropertySafe("headerName").Value;
                    return CollectCustomSecuritySchemeHeader(schemeName, headerName: (string)headerName);

                case JTokenType.String:
                    return CollectCustomSecuritySchemeBearer(schemeName);

                default:
                    throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, null);
            }
        }
        private bool CollectCustomSecurityScheme(string schemeName, SecuritySchemeValue value) => _securitySchemes.RegisterSecurityScheme(new SecurityScheme(schemeName, value));

        private bool CollectCustomSecuritySchemeBearer(string schemeName) => CollectCustomSecurityScheme(schemeName, new BearerSecuritySchemeValue());

        private bool CollectCustomSecuritySchemeHeader(string schemeName, string headerName) => CollectCustomSecurityScheme(schemeName, new HeaderSecuritySchemeValue(headerName));

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