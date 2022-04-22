using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private const string LockSectionName = "ControllerImport";
        private readonly IDictionary<string, SecurityScheme> _securitySchemeMap;
        private readonly IActionDefinitionResolverFacade _actionResolver;
        private readonly ICollection<string> _defaultSecuritySchemes;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly IActionParameterConverterRegistry _actionParameterConverterRegistry;
        private readonly LockEntryManager _lockEntryManager;
        #endregion

        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        public ICollection<SecurityScheme> SecuritySchemes { get; }
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider
        (
            IEnumerable<string> endpoints
          , ICollection<string> defaultSecuritySchemes
          , IDictionary<string, SecurityScheme> securitySchemeMap
          , IActionDefinitionResolverFacade actionResolver
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , LockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
        ) : base(fileSystemProvider, logger)
        {
            this._securitySchemeMap = securitySchemeMap;
            this._actionResolver = actionResolver;
            this._defaultSecuritySchemes = defaultSecuritySchemes;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._actionParameterSourceRegistry = actionParameterSourceRegistry;
            this._actionParameterConverterRegistry = actionParameterConverterRegistry;
            this._lockEntryManager = lockEntryManager;
            this.Controllers = new Collection<ControllerDefinition>();
            this.SecuritySchemes = new Collection<SecurityScheme>();
            this.Collect(endpoints);
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json) => this.ReadControllers(filePath, json);
        #endregion

        #region Private Methods
        private void ReadControllers(string filePath, JObject apis)
        {
            foreach (JProperty apiProperty in apis.Properties())
            {
                if (apiProperty.Name == "$schema")
                    continue;

                this.ReadController(filePath, apiProperty.Name, (JArray)apiProperty.Value);
            }
        }

        private void ReadController(string filePath, string controllerName, JArray actions)
        {
            ControllerDefinition controller = new ControllerDefinition(controllerName);
            foreach (JToken action in actions)
            {
                switch (action.Type)
                {
                    case JTokenType.Object:
                        this.ReadControllerAction(filePath, controller, (JObject)action);
                        break;

                    case JTokenType.String:
                        ReadControllerImport(controller, action, filePath);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(action.Path, action.Type, null);
                }
            }
            this.Controllers.Add(controller);
        }

        private void ReadControllerAction(string filePath, ControllerDefinition controller, JObject action)
        {
            // Collect explicit parameters
            IDictionary<string, ExplicitParameter> explicitParameters = new Dictionary<string, ExplicitParameter>();
            CollectActionParameters(action, filePath, explicitParameters);

            // Collect path parameters
            IDictionary<string, PathParameter> pathParameters = new Dictionary<string, PathParameter>(StringComparer.OrdinalIgnoreCase);
            JProperty childRouteProperty = action.Property("childRoute");
            string childRoute = (string)childRouteProperty?.Value;
            if (childRouteProperty != null)
            {
                IEnumerable<Group> pathSegments = HttpParameterUtility.ExtractPathParameters(childRoute);
                foreach (Group pathSegment in pathSegments) 
                    pathParameters.Add(pathSegment.Value, new PathParameter(childRouteProperty, pathSegment));
            }

            // Collect body parameters
            ActionRequestBody requestBody = this.ReadBody(action, filePath);
            ICollection<string> bodyParameters = GetBodyProperties(requestBody?.Contract, this._schemaRegistry);

            // Resolve action target, parameters and create action definition
            ActionDefinition actionDefinition = this.CreateActionDefinition(action, filePath, explicitParameters, pathParameters, bodyParameters);
            if (actionDefinition == null)
                return;

            // Unfortunately we do not have any metadata on reflection targets
            if (!(actionDefinition.Target is ReflectionActionTarget))
            {
                // Validate explicit parameters
                foreach (ExplicitParameter explicitParameter in explicitParameters.Values)
                {
                    base.Logger.LogError(null, $"Parameter '{explicitParameter.Name}' not found on action: {actionDefinition.Target.OperationName}", filePath, explicitParameter.Line, explicitParameter.Column);
                }

                // Validate path parameters
                foreach (PathParameter pathSegment in pathParameters.Values)
                {
                    base.Logger.LogError(null, $"Undefined path parameter: {pathSegment.Name}", filePath, pathSegment.Line, pathSegment.Column);
                }
            }

            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            actionDefinition.Method = method;
            actionDefinition.OperationId = (string)action.Property("operationId")?.Value ?? actionDefinition.Target.OperationName;
            actionDefinition.Description = (string)action.Property("description")?.Value;
            actionDefinition.ChildRoute = childRoute;
            actionDefinition.RequestBody = requestBody;

            if (TryReadFileResponse(action, out ActionFileResponse fileResponse, out IJsonLineInfo fileResponseLocation))
                actionDefinition.SetFileResponse(fileResponse, filePath, fileResponseLocation.LineNumber, fileResponseLocation.LinePosition);

            this.CollectActionResponses(action, actionDefinition, filePath);
            this.CollectSecuritySchemes(action, actionDefinition, filePath);

            if (!actionDefinition.Responses.Any())
                actionDefinition.DefaultResponseType = null;

            if (!actionDefinition.SecuritySchemes.Any()) 
                actionDefinition.SecuritySchemes.Add(this.EnsureDefaultSecuritySchemes().ToArray());

            if (controller.Actions.TryAdd(actionDefinition))
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append(actionDefinition.Method.ToString().ToUpperInvariant())
              .Append(' ')
              .Append(controller.Name);

            if (!String.IsNullOrEmpty(actionDefinition.ChildRoute))
                sb.Append('/').Append(actionDefinition.ChildRoute);

            IJsonLineInfo lineInfo = action;
            base.Logger.LogError(null, $"Duplicate action registration: {sb}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private ActionRequestBody ReadBody(JObject action, string filePath)
        {
            JToken bodyValue = action.Property("body")?.Value;
            if (bodyValue == null) 
                return null;

            switch (bodyValue.Type)
            {
                case JTokenType.Object:
                    return this.ReadBodyValue((JObject)bodyValue, filePath);

                case JTokenType.String:
                    return this.ReadBodyValue((JValue)bodyValue, filePath);

                default:
                    throw new ArgumentOutOfRangeException(bodyValue.Path, bodyValue.Type, null);
            }
        }

        private ActionRequestBody ReadBodyValue(JObject value, string filePath)
        {
            JValue contractName = (JValue)value.Property("contract")?.Value;
            JToken mediaTypeJson = value.Property("mediaType")?.Value;
            string mediaType = (string)mediaTypeJson;
            string binder = (string)value.Property("binder")?.Value;

            if (mediaTypeJson != null && mediaType != HttpMediaType.Json)
            {
                IJsonLineInfo mediaTypeLocation = mediaTypeJson.GetLineInfo();
                return new ActionRequestBody(mediaType, ActionDefinitionUtility.CreateStreamTypeReference(filePath, mediaTypeLocation.LineNumber, mediaTypeLocation.LinePosition));
            }

            if (contractName == null)
            {
                IJsonLineInfo valueLocation = value;
                base.Logger.LogError(null, "Body is missing 'contract' property", filePath, valueLocation.LineNumber, valueLocation.LinePosition);
                return null;
            }

            TypeReference contract = this.ResolveType(contractName, filePath);
            return new ActionRequestBody(mediaType, contract, binder);
        }
        private ActionRequestBody ReadBodyValue(JValue value, string filePath)
        {
            TypeReference contract = this.ResolveType(value, filePath);
            return new ActionRequestBody(contract);
        }

        private static ICollection<string> GetBodyProperties(TypeReference bodyContract, ISchemaRegistry schemaRegistry)
        {
            HashSet<string> properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bodyContract is SchemaTypeReference schemaTypeReference)
            {
                SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                if (schema is ObjectSchema objectSchema)
                    properties.AddRange(objectSchema.Properties.Select(x => x.Name));
            }
            return properties;
        }

        private void ReadControllerImport(ControllerDefinition controller, JToken value, string filePath)
        {
            string typeName = (string)value;
            if (!this._lockEntryManager.HasEntry(LockSectionName, typeName))
            {
                IJsonLineInfo lineInfo = value.GetLineInfo();
                base.Logger.LogError(null, "Controller imports are not supported anymore", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                return;
            }

            controller.ControllerImports.Add(typeName);
        }

        private void CollectActionParameters(JObject action, string filePath, IDictionary<string, ExplicitParameter> target)
        {
            JObject mappings = (JObject)action.Property("params")?.Value;
            if (mappings == null)
                return;

            foreach (JProperty property in mappings.Properties())
            {
                if (!TryReadSource(property, filePath, isItem: false, out ActionParameterSource source))
                {
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            source = ReadComplexActionParameter(property.Name, (JObject)property.Value, filePath);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Value.Path), property.Value.Type, null);
                    }
                }

                ExplicitParameter parameter = new ExplicitParameter(property, source);
                target.Add(property.Name, parameter);
            }
        }

        private bool TryReadSource(JProperty property, string filePath, bool isItem, out ActionParameterSource source)
        {
            switch (property.Value.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Null:
                    source = ReadConstantSource((JValue)property.Value);
                    return true;

                case JTokenType.String:
                    source = ReadPropertySource((JValue)property.Value, filePath);
                    return true;

                default:
                    source = null;
                    return false;
            }
        }

        private static ActionParameterSource ReadConstantSource(JValue value) => new ActionParameterConstantSource(value.Value);

        private ActionParameterPropertySource ReadPropertySource(JValue value, string filePath)
        {
            string[] parts = ((string)value.Value).Split(new[] { '.' }, 2);
            string sourceName = parts[0];
            string propertyName = parts[1];
            IJsonLineInfo valueLocation = value.GetLineInfo();

            if (!this._actionParameterSourceRegistry.TryGetDefinition(sourceName, out ActionParameterSourceDefinition definition))
            {
                base.Logger.LogError(null, $"Unknown property source '{sourceName}'", filePath, valueLocation.LineNumber, valueLocation.LinePosition);
            }

            ActionParameterPropertySource propertySource = new ActionParameterPropertySource(definition, propertyName, filePath, valueLocation.LineNumber, valueLocation.LinePosition);
            return propertySource;
        }

        private ActionParameterSource ReadComplexActionParameter(string parameterName, JObject container, string filePath)
        {
            JProperty bodyConverterProperty = container.Property("convertFromBody");
            if (bodyConverterProperty != null)
            {
                return ReadBodyActionParameter(bodyConverterProperty);
            }

            JProperty sourceProperty = container.Property("source");
            if (sourceProperty != null)
            {
                return ReadPropertyActionParameter(parameterName, container, sourceProperty, filePath);
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private ActionParameterSource ReadPropertyActionParameter(string parameterName, JObject container, JProperty sourceProperty, string filePath)
        {
            ActionParameterPropertySource propertySource = ReadPropertySource((JValue)sourceProperty.Value, filePath);

            JProperty itemsProperty = container.Property("items");
            if (itemsProperty != null)
            {
                JObject itemsObject = (JObject)itemsProperty.Value;
                foreach (JProperty itemProperty in itemsObject.Properties())
                {
                    if (TryReadSource(itemProperty, filePath, isItem: true, out ActionParameterSource itemPropertySource))
                        propertySource.ItemSources.Add(itemProperty.Name, itemPropertySource);
                }
                return propertySource;
            }

            JProperty converterProperty = container.Property("converter");
            if (converterProperty != null)
            {
                JToken converter = converterProperty.Value;
                string converterName = (string)converter;
                if (!this._actionParameterConverterRegistry.IsRegistered(converterName))
                {
                    IJsonLineInfo converterLocation = converter.GetLineInfo();
                    base.Logger.LogError(null, $"Unknown property converter '{converterName}'", filePath, converterLocation.LineNumber, converterLocation.LinePosition);
                }
                propertySource.Converter = converterName;
                return propertySource;
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static ActionParameterBodySource ReadBodyActionParameter(JProperty bodyConverterProperty)
        {
            string bodyConverterTypeName = (string)((JValue)bodyConverterProperty.Value).Value;
            return new ActionParameterBodySource(bodyConverterTypeName);
        }

        private static bool TryReadFileResponse(JObject action, out ActionFileResponse fileResponse, out IJsonLineInfo location)
        {
            JProperty fileResponseProperty = action.Property("fileResponse");
            JObject fileResponseValue = (JObject)fileResponseProperty?.Value;
            if (fileResponseValue == null)
            {
                fileResponse = null;
                location = null;
                return false;
            }

            JProperty mediaTypeProperty = fileResponseValue.Property("mediaType");
            if (mediaTypeProperty == null)
                throw new InvalidOperationException("Missing required property fileResponse.mediaType");

            JProperty cacheProperty = fileResponseValue.Property("cache");

            string mediaType = (string)mediaTypeProperty.Value;
            fileResponse = new ActionFileResponse(mediaType);
            location = fileResponseProperty.GetLineInfo();

            if (cacheProperty != null)
                fileResponse.Cache = (bool)cacheProperty.Value;

            return true;
        }

        private ActionDefinition CreateActionDefinition(JObject action, string filePath, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters)
        {
            JValue targetValue = (JValue)action.Property("target").Value;
            IJsonLineInfo lineInfo = targetValue.GetLineInfo();
            ActionDefinition actionDefinition = this._actionResolver.Resolve(targetName: (string)targetValue, filePath, lineInfo.LineNumber, lineInfo.LinePosition, explicitParameters, pathParameters, bodyParameters);
            return actionDefinition;
        }

        private void CollectActionResponses(JObject actionJson, ActionDefinition actionDefinition, string filePath)
        {
            JObject responses = (JObject)actionJson.Property("responses")?.Value;
            if (responses == null)
                return;

            foreach (JProperty property in responses.Properties())
            {
                HttpStatusCode statusCode = (HttpStatusCode)Int32.Parse(property.Name);

                if (!actionDefinition.Responses.TryGetValue(statusCode, out ActionResponse response))
                {
                    response = new ActionResponse(statusCode);
                    actionDefinition.Responses.Add(statusCode, response);
                }

                switch (property.Value.Type)
                {
                    case JTokenType.Null: continue;

                    case JTokenType.String when response.ResultType == null:
                        response.ResultType = this.ResolveType((JValue)property.Value, filePath);
                        break;

                    case JTokenType.Object:
                        JObject responseObject = (JObject)property.Value;
                        string description = (string)responseObject.Property("description")?.Value;

                        if (description != null)
                            response.Description = description;

                        if (response.ResultType == null)
                        {
                            JValue typeNameValue = (JValue)responseObject.Property("type")?.Value;
                            if (typeNameValue != null)
                                response.ResultType = this.ResolveType(typeNameValue, filePath);
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException(property.Value.Path, property.Value.Type, null);
                }
            }
        }

        private void CollectSecuritySchemes(JObject actionJson, ActionDefinition actionDefinition, string filePath)
        {
            JProperty property = actionJson.Property("authorization");
            if (property == null)
                return;

            switch (property.Value.Type)
            {
                case JTokenType.String:
                    this.CollectSecurityScheme(actionDefinition, property.Value, filePath);
                    break;

                case JTokenType.Array:
                    JArray array = (JArray)property.Value;
                    foreach (JToken value in array)
                        this.CollectSecurityScheme(actionDefinition, value, filePath);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(property.Value.Path, property.Value.Type, null);
            }
        }

        private void CollectSecurityScheme(ActionDefinition actionDefinition, JToken value, string filePath)
        {
            if (value.Type != JTokenType.String)
                throw new ArgumentOutOfRangeException(value.Path, value.Type, null);

            string name = (string)value;
            if (!this._securitySchemeMap.ContainsKey(name))
            {
                if (!CodeGeneration.SecuritySchemes.TryFindSecurityScheme(name, out SecurityScheme scheme)
                 && !this.TryGetDefaultSecurityScheme(name, out scheme))
                {
                    IJsonLineInfo lineInfo = value;
                    string possibleValues = String.Join(", ", CodeGeneration.SecuritySchemes
                                                                            .Schemes
                                                                            .Select(x => x.Name)
                                                                            .Concat(this._defaultSecuritySchemes));
                    base.Logger.LogError(null, $"Unknown authorization scheme '{name}'. Possible values are: {possibleValues}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                    return;
                }
                this._securitySchemeMap.Add(name, scheme);
            }

            actionDefinition.SecuritySchemes.Add(new[] { name });
        }

        private bool TryGetDefaultSecurityScheme(string name, out SecurityScheme scheme)
        {
            if (this._defaultSecuritySchemes.Contains(name))
            {
                scheme = CreateDefaultSecurityScheme(name);
                return true;
            }

            scheme = null;
            return false;
        }

        private IEnumerable<string> EnsureDefaultSecuritySchemes()
        {
            foreach (string name in this._defaultSecuritySchemes)
            {
                if (!this._securitySchemeMap.ContainsKey(name))
                {
                    SecurityScheme scheme = CreateDefaultSecurityScheme(name);
                    this._securitySchemeMap.Add(name, scheme);
                }
                yield return name;
            }
        }

        private static SecurityScheme CreateDefaultSecurityScheme(string name) => new SecurityScheme(name, SecuritySchemeKind.ApiKey);

        private TypeReference ResolveType(JValue typeNameValue, string filePath)
        {
            string typeName = (string)typeNameValue;
            IJsonLineInfo typeNameLocation = typeNameValue.GetLineInfo();
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            TypeReference type = this._typeResolver.ResolveType(typeName, null, filePath, typeNameLocation.LineNumber, typeNameLocation.LinePosition, isEnumerable);
            return type;
        }
        #endregion
    }
}