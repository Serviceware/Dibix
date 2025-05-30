﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : ValidatingJsonDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private readonly IDictionary<string, SecurityScheme> _usedSecuritySchemes;
        private readonly SecuritySchemes _securitySchemes;
        private readonly ConfigurationTemplates _templates;
        private readonly IActionTargetDefinitionResolverFacade _actionTargetResolver;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly RootParameterSourceReader _parameterSourceReader;
        #endregion

        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        public ICollection<SecurityScheme> SecuritySchemes => _usedSecuritySchemes.Values;
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider
        (
            IEnumerable<TaskItem> endpoints
          , SecuritySchemes securitySchemes
          , ConfigurationTemplates templates
          , IActionTargetDefinitionResolverFacade actionTargetResolver
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , ILogger logger
        ) : base(logger)
        {
            _usedSecuritySchemes = new Dictionary<string, SecurityScheme>();
            _securitySchemes = securitySchemes;
            _templates = templates;
            _actionTargetResolver = actionTargetResolver;
            _typeResolver = typeResolver;
            _schemaRegistry = schemaRegistry;
            _parameterSourceReader = new RootParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);
            Controllers = new Collection<ControllerDefinition>();
            Collect(endpoints.Select(x => x.GetFullPath()));
        }
        #endregion

        #region Overrides
        protected override void Read(JObject json) => ReadControllers(json);
        #endregion

        #region Private Methods
        private void ReadControllers(JObject apis)
        {
            foreach (JProperty apiProperty in apis.Properties())
            {
                if (apiProperty.Name == "$schema")
                    continue;

                ReadController(apiProperty.Name, (JArray)apiProperty.Value);
            }
        }

        private void ReadController(string controllerName, JArray actions)
        {
            ControllerDefinition controller = new ControllerDefinition(controllerName);
            foreach (JToken action in actions)
                ReadControllerItem(action, action.Type, controller);

            Controllers.Add(controller);
        }

        private void ReadControllerItem(JToken action, JTokenType type, ControllerDefinition controller)
        {
            switch (type)
            {
                case JTokenType.Object:
                    ReadControllerAction(controller, (JObject)action);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, action.Path);
            }
        }

        private void ReadControllerAction(ControllerDefinition controller, JObject action)
        {
            JObject actionMerged;
            if (_templates.Default.Action != null)
            {
                JObject actionTemplate = (JObject)_templates.Default.Action.DeepClone();
                actionTemplate.Merge(action, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                actionMerged = actionTemplate;
            }
            else
            {
                actionMerged = action;
            }

            // Collect body parameters
            ActionRequestBody requestBody = ReadBody(action);
            ICollection<string> bodyParameters = GetBodyProperties(requestBody?.Contract, _schemaRegistry);

            // Collect path parameters
            Token<string> childRoute = null;
            JProperty childRouteProperty = action.Property("childRoute");
            IReadOnlyDictionary<string, PathParameter> pathParameters;
            if (childRouteProperty != null)
            {
                string childRouteValue = (string)childRouteProperty.Value;
                childRoute = childRouteProperty?.Value.ToToken(childRouteValue);
                pathParameters = new ReadOnlyDictionary<string, PathParameter>(CollectPathParameters(childRouteProperty, childRouteValue).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                pathParameters = new ReadOnlyDictionary<string, PathParameter>(new Dictionary<string, PathParameter>());
            }

            // Collect explicit parameters
            IReadOnlyDictionary<string, ExplicitParameter> explicitParameters = CollectExplicitParameters(action, requestBody, pathParameters);

            // Resolve action target, parameters and create action definition
            ActionDefinition actionDefinition = CreateActionDefinition(action, explicitParameters, pathParameters, bodyParameters, requestBody, actionTargetDefinitionFactory: x => new ActionDefinition(x));
            if (actionDefinition == null)
                return;

            ActionTarget actionTarget = actionDefinition.Target;

            // Unfortunately we do not have any metadata on reflection targets
            if (actionTarget is not ReflectionActionTarget)
            {
                // Validate explicit parameters
                foreach (ExplicitParameter explicitParameter in explicitParameters.Values.Where(x => !x.Visited))
                {
                    Logger.LogError($"Parameter '{explicitParameter.Name}' not found on action: {actionTarget.OperationName}", explicitParameter.SourceLocation.Source, explicitParameter.SourceLocation.Line, explicitParameter.SourceLocation.Column);
                }

                // Validate path parameters
                foreach (PathParameter pathSegment in pathParameters.Values.Where(x => !x.Visited))
                {
                    Logger.LogError($"Undefined path parameter: {pathSegment.Name}", pathSegment.Location.Source, pathSegment.Location.Line, pathSegment.Location.Column);
                }
            }

            _ = Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            actionDefinition.Location = action.GetSourceInfo();
            actionDefinition.Method = method;
            actionDefinition.OperationId = (string)action.Property("operationId")?.Value ?? actionTarget.OperationName;
            actionDefinition.Description = (string)action.Property("description")?.Value;
            actionDefinition.RequestBody = requestBody;
            actionDefinition.ChildRoute = childRoute;

            if (TryReadFileResponse(action, out ActionFileResponse fileResponse, out SourceLocation fileResponseLocation))
                actionDefinition.SetFileResponse(fileResponse, fileResponseLocation);

            SourceLocation actionLineInfo = action.GetSourceInfo();
            CollectAuthorization(actionMerged, actionDefinition, actionLineInfo, pathParameters);
            CollectActionResponses(action, actionDefinition);
            CollectSecuritySchemes(actionMerged, actionDefinition, actionLineInfo);

            if (actionTarget is ReflectionActionTarget)
            {
                LogNotSupportedInHttpHostWarning($"Action target method '{actionTarget.OperationName}' is defined within an external assembly", actionTarget.SourceLocation);
                actionDefinition.CompatibilityLevel = ActionCompatibilityLevel.Reflection;
            }

            foreach (ActionParameter actionParameter in actionDefinition.Parameters)
            {
                if (actionParameter.IsOutput)
                {
                    LogNotSupportedInHttpHostWarning($"Parameter '{actionParameter.InternalParameterName}' is an output parameter", actionParameter.SourceLocation);
                    actionDefinition.CompatibilityLevel = ActionCompatibilityLevel.Reflection;
                }

                if (actionParameter.ParameterSource is ActionParameterBodySource { ConverterName: not null } bodySource)
                {
                    LogNotSupportedInHttpHostWarning($"Parameter '{actionParameter.InternalParameterName}' uses a converter", bodySource.ConverterName.Location);
                    actionDefinition.CompatibilityLevel = ActionCompatibilityLevel.Reflection;
                }
            }

            controller.Actions.Add(actionDefinition);
        }

        private void LogNotSupportedInHttpHostWarning(string message, SourceLocation sourceLocation) => Logger.LogWarning($"{message}, which is not supported in Dibix.Http.Host", sourceLocation);

        private ActionRequestBody ReadBody(JObject action)
        {
            JToken bodyValue = action.Property("body")?.Value;
            if (bodyValue == null)
                return null;

            return ReadBodyValue(bodyValue, bodyValue.Type);
        }

        private ActionRequestBody ReadBodyValue(JToken value, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    return ReadBodyValue((JObject)value);

                case JTokenType.String:
                    return ReadBodyValue((JValue)value);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);
            }
        }
        private ActionRequestBody ReadBodyValue(JObject value)
        {
            JValue contractName = (JValue)value.Property("contract")?.Value;
            JToken mediaTypeJson = value.Property("mediaType")?.Value;
            string mediaType = (string)mediaTypeJson;
            string binder = (string)value.Property("binder")?.Value;

            if (mediaTypeJson != null && mediaType != HttpMediaType.Json)
            {
                SourceLocation mediaTypeLocation = mediaTypeJson.GetSourceInfo();
                return new ActionRequestBody(mediaType, ActionDefinitionUtility.CreateStreamTypeReference(mediaTypeLocation));
            }

            if (contractName == null)
            {
                SourceLocation valueLocation = value.GetSourceInfo();
                Logger.LogError("Body is missing 'contract' property", valueLocation.Source, valueLocation.Line, valueLocation.Column);
                return null;
            }

            TypeReference contract = contractName.ResolveType(_typeResolver);
            return new ActionRequestBody(mediaType, contract, binder);
        }
        private ActionRequestBody ReadBodyValue(JValue value)
        {
            TypeReference contract = value.ResolveType(_typeResolver);
            return new ActionRequestBody(contract);
        }

        private static ICollection<string> GetBodyProperties(TypeReference bodyContract, ISchemaRegistry schemaRegistry)
        {
            HashSet<string> properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bodyContract is SchemaTypeReference schemaTypeReference)
            {
                SchemaDefinition schema = schemaRegistry.GetSchema(schemaTypeReference);
                if (schema is ObjectSchema objectSchema)
                    properties.AddRange(objectSchema.Properties.Select(x => x.Name.Value));
            }
            return properties;
        }

        private IReadOnlyDictionary<string, ExplicitParameter> CollectExplicitParameters(JObject action, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            IDictionary<string, ExplicitParameter> target = new Dictionary<string, ExplicitParameter>();
            CollectExplicitParameters(action, target, requestBody, pathParameters);
            IReadOnlyDictionary<string, ExplicitParameter> explicitParameters = new ReadOnlyDictionary<string, ExplicitParameter>(target);
            return explicitParameters;
        }
        private void CollectExplicitParameters(JObject action, IDictionary<string, ExplicitParameter> target, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            JObject mappings = (JObject)action.Property("params")?.Value;
            if (mappings == null)
                return;

            foreach (JProperty property in mappings.Properties())
            {
                ExplicitParameter parameter = CollectExplicitParameter(property, requestBody, pathParameters);
                if (parameter == null)
                    continue;

                target.Add(property.Name, parameter);
            }
        }

        private ExplicitParameter CollectExplicitParameter(JProperty property, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            TypeReference type = null;
            ActionParameterLocation? parameterLocation = null;
            Func<TypeReference, ValueReference> defaultValueResolver = null;
            bool isPathParameter = pathParameters.TryGetValue(property.Name, out PathParameter pathParameter);

            if (isPathParameter)
                pathParameter.Visited = true;

            if (property.Value.Type == JTokenType.Object)
            {
                JObject properties = (JObject)property.Value;
                JValue typeValue = (JValue)properties.Property("type")?.Value;
                if (typeValue != null)
                    type = typeValue.ResolveType(_typeResolver);

                JProperty locationProperty = properties.Property("location");
                string parameterLocationValue = (string)locationProperty?.Value;

                if (parameterLocationValue != null)
                {
                    if (isPathParameter)
                        Logger.LogError($"Parameter '{property.Name}' is resolved from path and therefore must not have an explicit location property", locationProperty.GetSourceInfo());

                    parameterLocation = (ActionParameterLocation)Enum.Parse(typeof(ActionParameterLocation), parameterLocationValue);
                }

                JValue defaultValue = (JValue)properties.Property("default")?.Value;
                if (defaultValue != null)
                    defaultValueResolver = x => JsonValueReferenceParser.Parse(x, defaultValue, _schemaRegistry, Logger);
            }

            ActionParameterSourceBuilder parameterSourceBuilder = CollectRootParameterSource(property, requestBody, pathParameters);
            return new ExplicitParameter(property, type, parameterLocation, defaultValueResolver, parameterSourceBuilder) { Visited = isPathParameter };
        }

        private static IEnumerable<KeyValuePair<string, PathParameter>> CollectPathParameters(JProperty childRouteProperty, string childRouteValue)
        {
            IEnumerable<Group> pathSegments = HttpParameterUtility.ExtractPathParameters(childRouteValue);
            foreach (Group pathSegment in pathSegments)
                yield return new KeyValuePair<string, PathParameter>(pathSegment.Value, new PathParameter(childRouteProperty, pathSegment));
        }

        private ActionParameterSourceBuilder CollectRootParameterSource(JProperty property, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            return _parameterSourceReader.Read(property, requestBody, pathParameters, rootParameterSourceBuilder: null);
        }

        private static bool TryReadFileResponse(JObject action, out ActionFileResponse fileResponse, out SourceLocation location)
        {
            JProperty fileResponseProperty = action.Property("fileResponse");
            JObject fileResponseValue = (JObject)fileResponseProperty?.Value;
            if (fileResponseValue == null)
            {
                fileResponse = null;
                location = default;
                return false;
            }

            JProperty mediaTypeProperty = fileResponseValue.GetPropertySafe("mediaType");
            JProperty cacheProperty = fileResponseValue.Property("cache");

            string mediaType = (string)mediaTypeProperty.Value;
            fileResponse = new ActionFileResponse(mediaType);
            location = fileResponseProperty.GetSourceInfo();

            if (cacheProperty != null)
                fileResponse.Cache = (bool)cacheProperty.Value;

            return true;
        }

        private void CollectActionResponses(JObject actionJson, ActionDefinition actionDefinition)
        {
            JProperty responseProperty = actionJson.Property("response");
            if (responseProperty == null)
                return;

            CollectActionResponses(responseProperty, responseProperty.Value.Type, actionDefinition);
        }
        private void CollectActionResponses(JProperty responseProperty, JTokenType type, ActionDefinition actionDefinition)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JObject responseObject = (JObject)responseProperty.Value;
                    ICollection<JProperty> properties = responseObject.Properties().ToArray();
                    if (properties.Any(x => x.Name is "type" or "description" or "autoDetect"))
                    {
                        CollectActionResponseFromStatusCode(HttpStatusCode.OK, responseProperty.Value, responseProperty.Value.Type, actionDefinition);
                        return;
                    }

                    foreach (JProperty property in properties)
                        CollectActionResponseFromStatusCode(property, property.Value.Type, actionDefinition);

                    break;

                case JTokenType.Null:
                    actionDefinition.DefaultResponseType = null;
                    break;

                case JTokenType.String:
                    CollectActionResponseFromStatusCode(HttpStatusCode.OK, responseProperty.Value, responseProperty.Value.Type, actionDefinition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void CollectActionResponseFromStatusCode(JProperty statusCodeProperty, JTokenType type, ActionDefinition actionDefinition)
        {
            HttpStatusCode statusCode = (HttpStatusCode)Int32.Parse(statusCodeProperty.Name);
            CollectActionResponseFromStatusCode(statusCode, statusCodeProperty.Value, type, actionDefinition);
        }
        private void CollectActionResponseFromStatusCode(HttpStatusCode statusCode, JToken value, JTokenType type, ActionDefinition actionDefinition)
        {
            if (!actionDefinition.Responses.TryGetValue(statusCode, out ActionResponse response))
            {
                response = new ActionResponse(statusCode);
                actionDefinition.Responses.Add(statusCode, response);
            }

            switch (type)
            {
                case JTokenType.Null: return;

                case JTokenType.String when response.ResultType == null:
                    response.ResultType = ((JValue)value).ResolveType(_typeResolver);
                    break;

                case JTokenType.Object:
                    CollectActionResponseDetail((JObject)value, actionDefinition, response);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);
            }
        }

        private void CollectActionResponseDetail(JObject responseObject, ActionDefinition actionDefinition, ActionResponse response)
        {
            string description = (string)responseObject.Property("description")?.Value;
            JProperty autoDetectProperty = responseObject.Property("autoDetect");

            if (response.ResultType == null)
            {
                JValue typeNameValue = (JValue)responseObject.Property("type")?.Value;
                if (typeNameValue != null)
                    response.ResultType = typeNameValue.ResolveType(_typeResolver);
            }

            if (description != null)
                response.Description = description;

            if (autoDetectProperty != null)
                CollectStatusCodeDetection(autoDetectProperty, autoDetectProperty.Value.Type, actionDefinition, response);
        }

        private void CollectStatusCodeDetection(JProperty property, JTokenType type, ActionDefinition actionDefinition, ActionResponse response)
        {
            switch (type)
            {
                case JTokenType.Boolean:
                    actionDefinition.DisabledAutoDetectionStatusCodes.Add((int)response.StatusCode);

                    if (property.Parent!.Count == 1)
                        actionDefinition.Responses.Remove(response.StatusCode);

                    break;

                case JTokenType.Object:
                    JObject autoDetectObject = (JObject)property.Value;
                    int errorCode = (int?)autoDetectObject.Property("errorCode")?.Value ?? default;
                    string errorMessage = (string)autoDetectObject.Property("errorMessage")?.Value;
                    SourceLocation sourceLocation = autoDetectObject.GetSourceInfo();
                    ErrorDescription error = new ErrorDescription(errorCode, errorMessage, sourceLocation);
                    response.AddError(errorCode, error.Description, sourceLocation, Logger);
                    response.StatusCodeDetectionDetail = error;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }

        private void CollectSecuritySchemes(JObject actionJson, ActionDefinition actionDefinition, SourceLocation actionLineInfo)
        {
            const string propertyName = "securitySchemes";
            JProperty property = actionJson.Property(propertyName);
            if (property == null)
            {
                Logger.LogError($"Missing required property '{propertyName}'", actionLineInfo.Source, actionLineInfo.Line, actionLineInfo.Column);
                return;
            }

            IEnumerable<SecurityScheme> schemes = CollectSecurityScheme(property, property.Value.Type);
            actionDefinition.SecuritySchemes.Requirements.AddRange(schemes.Select(x => new SecuritySchemeRequirement(x)));
        }

        private IEnumerable<SecurityScheme> CollectSecurityScheme(JProperty property, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.String:
                    foreach (SecurityScheme securityScheme in CollectSecurityScheme(property.Value, property.Value.Type))
                        yield return securityScheme;

                    break;

                case JTokenType.Array:
                    JArray array = (JArray)property.Value;
                    foreach (SecurityScheme securityScheme in array.SelectMany(x => CollectSecurityScheme(x, x.Type)))
                        yield return securityScheme;

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }
        private IEnumerable<SecurityScheme> CollectSecurityScheme(JToken value, JTokenType type)
        {
            if (type != JTokenType.String)
                throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);

            string name = (string)value;
            if (!_usedSecuritySchemes.TryGetValue(name, out SecurityScheme scheme))
            {
                if (!_securitySchemes.TryFindSecurityScheme(name, out scheme))
                {
                    SourceLocation sourceInfo = value.GetSourceInfo();
                    string possibleValues = String.Join(", ", _securitySchemes.Schemes.Select(x => x.SchemeName));
                    Logger.LogError($"Unknown authorization scheme '{name}'. Possible values are: {possibleValues}", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                    yield break;
                }
                _usedSecuritySchemes.Add(name, scheme);
            }
            yield return scheme;
        }

        private void CollectAuthorization(JObject actionJson, ActionDefinition actionDefinition, SourceLocation actionLineInfo, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            const string propertyName = "authorization";
            JProperty property = actionJson.Property(propertyName);
            if (property == null)
            {
                Logger.LogError($"Missing required property '{propertyName}'", actionLineInfo.Source, actionLineInfo.Line, actionLineInfo.Column);
                return;
            }

            CollectAuthorization(property.Value.Type, property.Value, actionDefinition, pathParameters);
        }
        private void CollectAuthorization(JTokenType type, JToken value, ActionDefinition actionDefinition, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JObject authorizationValue = (JObject)value;
                    JToken templateNameValue = authorizationValue.Property("name")?.Value;
                    CollectAuthorization(templateNameValue, authorizationValue, actionDefinition, pathParameters);
                    break;

                case JTokenType.String when (string)value == "none":
                    break;

                case JTokenType.String:
                    CollectAuthorization(templateNameValue: value, authorizationValue: new JObject(), actionDefinition, pathParameters);
                    break;

                case JTokenType.Array:
                    JArray array = (JArray)value;
                    foreach (JToken item in array)
                        CollectAuthorization(item.Type, item, actionDefinition, pathParameters);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);
            }
        }
        private void CollectAuthorization(JToken templateNameValue, JObject authorizationValue, ActionDefinition actionDefinition, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            JObject authorization = authorizationValue;

            // "name" property is optional, when the endpoint defines an authorization behavior manually
            if (templateNameValue != null)
            {
                authorization = ApplyAuthorizationTemplate(templateNameValue, authorizationValue);
                if (authorization == null) // If the template is not found, it will have been logged already at this point
                    return;
            }

            IReadOnlyDictionary<string, ExplicitParameter> explicitParameters = CollectExplicitParameters(authorization, requestBody: null, pathParameters);
            ICollection<string> bodyParameters = new Collection<string>();
            AuthorizationBehavior authorizationBehavior = CreateActionDefinition(authorization, explicitParameters, pathParameters, bodyParameters, requestBody: null, actionTargetDefinitionFactory: actionTarget => new AuthorizationBehavior(actionDefinition, actionTarget));
            actionDefinition.Authorization.Add(authorizationBehavior);
        }

        private JObject ApplyAuthorizationTemplate(JToken templateNameValue, JObject authorizationTemplateReference)
        {
            string templateName = (string)templateNameValue;
            if (!_templates.Authorization.TryGetTemplate(templateName, out ConfigurationAuthorizationTemplate template))
            {
                SourceLocation templateNameLineInfo = templateNameValue.GetSourceInfo();
                Logger.LogError($"Unknown authorization template '{templateName}'", templateNameLineInfo.Source, templateNameLineInfo.Line, templateNameLineInfo.Column);
                return null;
            }

            JObject resolvedAuthorization = new JObject();

            if (authorizationTemplateReference.HasValues)
            {
                JObject @params = new JObject();
                resolvedAuthorization.Add(new JProperty("params", @params));

                foreach (JProperty authorizationParameterProperty in authorizationTemplateReference.Properties())
                {
                    @params.Add(authorizationParameterProperty);
                }
            }

            JObject mergedAuthorization = (JObject)template.Content.DeepClone();
            mergedAuthorization.Merge(resolvedAuthorization);
            return mergedAuthorization;
        }

        private T CreateActionDefinition<T>(JObject action, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody, Func<ActionTarget, T> actionTargetDefinitionFactory) where T : ActionTargetDefinition
        {
            JValue targetValue = (JValue)action.Property("target").Value;
            SourceLocation sourceLocation = targetValue.GetSourceInfo();
            T actionDefinition = _actionTargetResolver.Resolve(targetName: (string)targetValue, sourceLocation, explicitParameters, pathParameters, bodyParameters, requestBody, actionTargetDefinitionFactory);
            return actionDefinition;
        }
        #endregion
    }
}