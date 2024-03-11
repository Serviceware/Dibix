using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : ValidatingJsonDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private const string LockSectionName = "ControllerImport";
        private readonly IDictionary<string, SecurityScheme> _usedSecuritySchemes;
        private readonly SecuritySchemes _securitySchemes;
        private readonly ConfigurationTemplates _templates;
        private readonly IActionTargetDefinitionResolverFacade _actionTargetResolver;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly IActionParameterConverterRegistry _actionParameterConverterRegistry;
        private readonly ILockEntryManager _lockEntryManager;
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
          , ILockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
        ) : base(fileSystemProvider, logger)
        {
            _usedSecuritySchemes = new Dictionary<string, SecurityScheme>();
            _securitySchemes = securitySchemes;
            _templates = templates;
            _actionTargetResolver = actionTargetResolver;
            _typeResolver = typeResolver;
            _schemaRegistry = schemaRegistry;
            _actionParameterSourceRegistry = actionParameterSourceRegistry;
            _actionParameterConverterRegistry = actionParameterConverterRegistry;
            _lockEntryManager = lockEntryManager;
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

                case JTokenType.String:
                    ReadControllerImport(controller, action);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, action.Path);
            }
        }

        private void ReadControllerAction(ControllerDefinition controller, JObject action)
        {
            JObject actionMerged = (JObject)_templates.Default.Action.DeepClone();
            actionMerged.Merge(action, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

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
            ActionDefinition actionDefinition = CreateActionDefinition<ActionDefinition>(action, explicitParameters, pathParameters, bodyParameters);
            if (actionDefinition == null)
                return;

            // Unfortunately we do not have any metadata on reflection targets
            if (actionDefinition.Target is not ReflectionActionTarget)
            {
                // Validate explicit parameters
                foreach (ExplicitParameter explicitParameter in explicitParameters.Values.Where(x => !x.Visited))
                {
                    base.Logger.LogError($"Parameter '{explicitParameter.Name}' not found on action: {actionDefinition.Target.OperationName}", explicitParameter.Location.Source, explicitParameter.Location.Line, explicitParameter.Location.Column);
                }

                // Validate path parameters
                foreach (PathParameter pathSegment in pathParameters.Values.Where(x => !x.Visited))
                {
                    base.Logger.LogError($"Undefined path parameter: {pathSegment.Name}", pathSegment.Location.Source, pathSegment.Location.Line, pathSegment.Location.Column);
                }
            }

            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            actionDefinition.Method = method;
            actionDefinition.OperationId = (string)action.Property("operationId")?.Value ?? actionDefinition.Target.OperationName;
            actionDefinition.Description = (string)action.Property("description")?.Value;
            actionDefinition.RequestBody = requestBody;
            actionDefinition.ChildRoute = childRoute;

            if (TryReadFileResponse(action, out ActionFileResponse fileResponse, out SourceLocation fileResponseLocation))
                actionDefinition.SetFileResponse(fileResponse, fileResponseLocation);

            SourceLocation actionLineInfo = action.GetSourceInfo();
            CollectActionResponses(action, actionDefinition);
            CollectSecuritySchemes(actionMerged, actionDefinition, actionLineInfo);
            CollectAuthorization(actionMerged, actionDefinition, actionLineInfo, pathParameters);

            if (!actionDefinition.Responses.Any())
                actionDefinition.DefaultResponseType = null;

            if (controller.Actions.TryAdd(actionDefinition))
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append(actionDefinition.Method.ToString().ToUpperInvariant())
              .Append(' ')
              .Append(controller.Name);

            if (actionDefinition.ChildRoute != null)
                sb.Append('/').Append(actionDefinition.ChildRoute);

            SourceLocation sourceInfo = action.GetSourceInfo();
            base.Logger.LogError($"Duplicate action registration: {sb}", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
        }

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
                base.Logger.LogError("Body is missing 'contract' property", valueLocation.Source, valueLocation.Line, valueLocation.Column);
                return null;
            }

            TypeReference contract = ResolveType(contractName);
            return new ActionRequestBody(mediaType, contract, binder);
        }
        private ActionRequestBody ReadBodyValue(JValue value)
        {
            TypeReference contract = ResolveType(value);
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

        private void ReadControllerImport(ControllerDefinition controller, JToken value)
        {
            string typeName = (string)value;
            if (!_lockEntryManager.HasEntry(LockSectionName, typeName))
            {
                SourceLocation sourceInfo = value.GetSourceInfo();
                base.Logger.LogError("Controller imports are not supported anymore", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                return;
            }

            controller.ControllerImports.Add(typeName);
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
                if (!TryReadSource(property, requestBody: requestBody, rootPropertySourceBuilder: null, pathParameters, out ActionParameterSourceBuilder parameterSourceBuilder))
                {
                    parameterSourceBuilder = CollectActionParameterSource(requestBody, property, property.Value.Type, pathParameters);
                }

                ExplicitParameter parameter = new ExplicitParameter(property, parameterSourceBuilder);
                target.Add(property.Name, parameter);
            }
        }

        private static IEnumerable<KeyValuePair<string, PathParameter>> CollectPathParameters(JProperty childRouteProperty, string childRouteValue)
        {
            IEnumerable<Group> pathSegments = HttpParameterUtility.ExtractPathParameters(childRouteValue);
            foreach (Group pathSegment in pathSegments)
                yield return new KeyValuePair<string, PathParameter>(pathSegment.Value, new PathParameter(childRouteProperty, pathSegment));
        }

        private ActionParameterSourceBuilder CollectActionParameterSource(ActionRequestBody requestBody, JProperty property, JTokenType type, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            switch (type)
            {
                case JTokenType.Object: return ReadComplexActionParameter(property.Name, (JObject)property.Value, requestBody, pathParameters);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }

        private bool TryReadSource(JProperty property, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootPropertySourceBuilder, IReadOnlyDictionary<string, PathParameter> pathParameters, out ActionParameterSourceBuilder parameterSourceBuilder)
        {
            switch (property.Value.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Null:
                    parameterSourceBuilder = new ActionParameterConstantSourceBuilder((JValue)property.Value, _schemaRegistry, Logger);
                    return true;

                case JTokenType.String:
                    string stringValue = (string)property.Value;
                    if (stringValue != null && stringValue.Contains('.'))
                    {
                        parameterSourceBuilder = ReadPropertySource((JValue)property.Value, requestBody, rootPropertySourceBuilder, pathParameters);
                    }
                    else
                    {
                        parameterSourceBuilder = new ActionParameterConstantSourceBuilder((JValue)property.Value, _schemaRegistry, Logger);
                    }
                    return true;

                default:
                    parameterSourceBuilder = null;
                    return false;
            }
        }

        private ActionParameterSourceBuilder ReadPropertySource(JValue value, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootParameterSourceBuilder, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            string[] parts = ((string)value.Value).Split(new[] { '.' }, 2);
            string sourceName = parts[0];
            string propertyName = parts[1];
            SourceLocation valueLocation = value.GetSourceInfo();

            if (!_actionParameterSourceRegistry.TryGetDefinition(sourceName, out ActionParameterSourceDefinition definition))
            {
                base.Logger.LogError($"Unknown property source '{sourceName}'", valueLocation.Source, valueLocation.Line, valueLocation.Column);
            }

            if (definition is ClaimParameterSource claimParameterSource)
                return new StaticActionParameterSourceBuilder(new ActionParameterClaimSource(claimParameterSource, propertyName, valueLocation));

            ActionParameterPropertySourceBuilder propertySourceBuilder = new ActionParameterPropertySourceBuilder(definition, propertyName, valueLocation);
            CollectPropertySourceNodes(propertySourceBuilder, requestBody, rootParameterSourceBuilder, pathParameters);
            return propertySourceBuilder;
        }

        private ActionParameterSourceBuilder ReadComplexActionParameter(string parameterName, JObject container, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            JProperty bodyConverterProperty = container.Property("convertFromBody");
            if (bodyConverterProperty != null)
            {
                return ReadBodyActionParameter(bodyConverterProperty);
            }

            JProperty sourceProperty = container.Property("source");
            if (sourceProperty != null)
            {
                return ReadPropertyActionParameter(parameterName, container, sourceProperty, requestBody, pathParameters);
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private ActionParameterSourceBuilder ReadPropertyActionParameter(string parameterName, JObject container, JProperty sourceProperty, ActionRequestBody requestBody, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            ActionParameterSourceBuilder sourceBuilder = ReadPropertySource((JValue)sourceProperty.Value, requestBody, rootParameterSourceBuilder: null, pathParameters);
            if (sourceBuilder is not ActionParameterPropertySourceBuilder propertySourceBuilder)
                return sourceBuilder;

            JProperty itemsProperty = container.Property("items");
            if (itemsProperty != null)
            {
                JObject itemsObject = (JObject)itemsProperty.Value;
                foreach (JProperty itemProperty in itemsObject.Properties())
                {
                    if (TryReadSource(itemProperty, requestBody: requestBody, rootPropertySourceBuilder: propertySourceBuilder, pathParameters, parameterSourceBuilder: out ActionParameterSourceBuilder itemPropertySourceBuilder))
                    {
                        SourceLocation sourceInfo = itemProperty.GetSourceInfo();
                        propertySourceBuilder.ItemSources.Add(new ActionParameterItemSourceBuilder(itemProperty.Name, itemPropertySourceBuilder, sourceInfo, _schemaRegistry, Logger));
                    }
                }
                return propertySourceBuilder;
            }

            JProperty converterProperty = container.Property("converter");
            if (converterProperty != null)
            {
                JToken converter = converterProperty.Value;
                string converterName = (string)converter;
                if (!_actionParameterConverterRegistry.IsRegistered(converterName))
                {
                    SourceLocation converterLocation = converter.GetSourceInfo();
                    base.Logger.LogError($"Unknown property converter '{converterName}'", converterLocation.Source, converterLocation.Line, converterLocation.Column);
                }
                propertySourceBuilder.Converter = converterName;
                return propertySourceBuilder;
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static ActionParameterSourceBuilder ReadBodyActionParameter(JProperty bodyConverterProperty)
        {
            string bodyConverterTypeName = (string)((JValue)bodyConverterProperty.Value).Value;
            return new StaticActionParameterSourceBuilder(new ActionParameterBodySource(bodyConverterTypeName));
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

            JProperty mediaTypeProperty = fileResponseValue.Property("mediaType");
            if (mediaTypeProperty == null)
                throw new InvalidOperationException("Missing required property fileResponse.mediaType");

            JProperty cacheProperty = fileResponseValue.Property("cache");

            string mediaType = (string)mediaTypeProperty.Value;
            fileResponse = new ActionFileResponse(mediaType);
            location = fileResponseProperty.GetSourceInfo();

            if (cacheProperty != null)
                fileResponse.Cache = (bool)cacheProperty.Value;

            return true;
        }

        private T CreateActionDefinition<T>(JObject action, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters) where T : ActionTargetDefinition, new()
        {
            JValue targetValue = (JValue)action.Property("target").Value;
            SourceLocation sourceInfo = targetValue.GetSourceInfo();
            T actionDefinition = _actionTargetResolver.Resolve<T>(targetName: (string)targetValue, sourceInfo, explicitParameters, pathParameters, bodyParameters);
            return actionDefinition;
        }

        private void CollectActionResponses(JObject actionJson, ActionDefinition actionDefinition)
        {
            JObject responses = (JObject)actionJson.Property("responses")?.Value;
            if (responses == null)
                return;

            foreach (JProperty property in responses.Properties()) 
                CollectActionResponse(property, property.Value.Type, actionDefinition);
        }

        private void CollectActionResponse(JProperty property, JTokenType type, ActionTargetDefinition actionTargetDefinition)
        {
            HttpStatusCode statusCode = (HttpStatusCode)Int32.Parse(property.Name);

            if (!actionTargetDefinition.Responses.TryGetValue(statusCode, out ActionResponse response))
            {
                response = new ActionResponse(statusCode);
                actionTargetDefinition.Responses.Add(statusCode, response);
            }

            switch (type)
            {
                case JTokenType.Null: return;

                case JTokenType.String when response.ResultType == null:
                    response.ResultType = ResolveType((JValue)property.Value);
                    break;

                case JTokenType.Object:
                    JObject responseObject = (JObject)property.Value;
                    string description = (string)responseObject.Property("description")?.Value;
                    JProperty autoDetectProperty = responseObject.Property("autoDetect");

                    if (response.ResultType == null)
                    {
                        JValue typeNameValue = (JValue)responseObject.Property("type")?.Value;
                        if (typeNameValue != null)
                            response.ResultType = ResolveType(typeNameValue);
                    }

                    if (description != null)
                        response.Description = description;
                    
                    if (autoDetectProperty != null)
                        CollectStatusCodeDetection(autoDetectProperty, autoDetectProperty.Value.Type, actionTargetDefinition, response);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }

        private static void CollectStatusCodeDetection(JProperty property, JTokenType type, ActionTargetDefinition actionTargetDefinition, ActionResponse response)
        {
            switch (type)
            {
                case JTokenType.Boolean:
                    actionTargetDefinition.DisabledAutoDetectionStatusCodes.Add((int)response.StatusCode);

                    if (property.Parent!.Count == 1)
                        actionTargetDefinition.Responses.Remove(response.StatusCode);

                    break;

                case JTokenType.Object:
                    JObject autoDetectObject = (JObject)property.Value;
                    int errorCode = (int?)autoDetectObject.Property("errorCode")?.Value ?? default;
                    string errorMessage = (string)autoDetectObject.Property("errorMessage")?.Value;
                    ErrorDescription error = new ErrorDescription(errorCode, errorMessage);
                    response.Errors[errorCode] = error;
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
                base.Logger.LogError($"Missing required property '{propertyName}'", actionLineInfo.Source, actionLineInfo.Line, actionLineInfo.Column);
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
                    string possibleValues = String.Join(", ", _securitySchemes.Schemes.Select(x => x.Name));
                    base.Logger.LogError($"Unknown authorization scheme '{name}'. Possible values are: {possibleValues}", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
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
                base.Logger.LogError($"Missing required property '{propertyName}'", actionLineInfo.Source, actionLineInfo.Line, actionLineInfo.Column);
                return;
            }

            CollectAuthorization(property, property.Value.Type, actionDefinition, pathParameters);
        }
        private void CollectAuthorization(JProperty property, JTokenType type, ActionDefinition actionDefinition, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JObject authorizationValue = (JObject)property.Value;
                    JProperty templateProperty = authorizationValue.Property("name");
                    CollectAuthorization(templateProperty, authorizationValue, actionDefinition, pathParameters);
                    break;

                case JTokenType.String when (string)property.Value == "none":
                    break;

                case JTokenType.String:
                    CollectAuthorization(templateProperty: property, authorizationValue: new JObject(), actionDefinition, pathParameters);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }
        private void CollectAuthorization(JProperty templateProperty, JObject authorizationValue, ActionDefinition actionDefinition, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            JObject authorization = authorizationValue;

            if (templateProperty != null
             && (authorization = ApplyAuthorizationTemplate(templateProperty, authorizationValue)) == null) // In case of error that has been previously logged
                return;

            IReadOnlyDictionary<string, ExplicitParameter> explicitParameters = CollectExplicitParameters(authorization, requestBody: null, pathParameters);
            ICollection<string> bodyParameters = new Collection<string>();
            actionDefinition.Authorization = CreateActionDefinition<AuthorizationBehavior>(authorization, explicitParameters, pathParameters, bodyParameters);
        }

        private JObject ApplyAuthorizationTemplate(JProperty templateNameProperty, JObject authorizationTemplateReference)
        {
            string templateName = (string)templateNameProperty.Value;
            if (!_templates.Authorization.TryGetTemplate(templateName, out ConfigurationAuthorizationTemplate template))
            {
                SourceLocation templateNameLineInfo = templateNameProperty.Value.GetSourceInfo();
                Logger.LogError($"Unknown authorization template '{templateName}'", templateNameLineInfo.Source, templateNameLineInfo.Line, templateNameLineInfo.Column);
                return null;
            }

            templateNameProperty.Remove();

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

        private TypeReference ResolveType(JValue typeNameValue)
        {
            string typeName = (string)typeNameValue;
            SourceLocation typeNameLocation = typeNameValue.GetSourceInfo();
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            TypeReference type = _typeResolver.ResolveType(typeName, null, typeNameLocation, isEnumerable);
            return type;
        }

        private void CollectPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootPropertySourceBuilder, IReadOnlyDictionary<string, PathParameter> pathParameters)
        {
            switch (propertySourceBuilder.Definition)
            {
                case BodyParameterSource _:
                    CollectBodyPropertySourceNodes(propertySourceBuilder, requestBody);
                    break;

                case ItemParameterSource _:
                    CollectItemPropertySourceNodes(propertySourceBuilder, rootPropertySourceBuilder);
                    break;

                default: 
                    return;
            }
        }

        private void CollectBodyPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, ActionRequestBody requestBody)
        {
            if (requestBody == null)
            {
                base.Logger.LogError("Must specify a body contract on the endpoint action when using BODY property source", propertySourceBuilder.Location.Source, propertySourceBuilder.Location.Line, propertySourceBuilder.Location.Column);
                return;
            }

            // Only traverse, if the body is an object contract
            TypeReference type = requestBody.Contract;
            if (type is not SchemaTypeReference bodySchemaTypeReference)
                return;

            if (_schemaRegistry.GetSchema(bodySchemaTypeReference) is not ObjectSchema objectSchema)
                return;

            IList<string> segments = propertySourceBuilder.PropertyName.Split('.');
            CollectPropertySourceNodes(propertySourceBuilder, segments, type, objectSchema);
        }

        private void CollectItemPropertySourceNodes(ActionParameterPropertySourceBuilder propertySource, ActionParameterPropertySourceBuilder rootPropertySourceBuilder)
        {
            if (!rootPropertySourceBuilder.Nodes.Any())
            {
                // Oops, a previous error should have been logged in this case
                return;
            }

            ActionParameterPropertySourceNode lastNode = rootPropertySourceBuilder.Nodes.LastOrDefault();
            if (lastNode == null)
                throw new InvalidOperationException($"Missing resolved source property node for item property mapping ({rootPropertySourceBuilder.PropertyName})");

            TypeReference type = lastNode.Property.Type;
            if (type is not SchemaTypeReference propertySchemaTypeReference)
            {
                base.Logger.LogError($"Unexpected type '{type?.GetType()}' for property '{rootPropertySourceBuilder.PropertyName}'. Only object schemas can be used for UDT item mappings.", rootPropertySourceBuilder.Location.Source, rootPropertySourceBuilder.Location.Line, rootPropertySourceBuilder.Location.Column);
                return;
            }

            SchemaDefinition propertySchema = _schemaRegistry.GetSchema(propertySchemaTypeReference);
            if (propertySchema is not ObjectSchema objectSchema)
            {
                base.Logger.LogError($"Unexpected type '{propertySchema?.GetType()}' for property '{rootPropertySourceBuilder.PropertyName}'. Only object schemas can be used for UDT item mappings.", rootPropertySourceBuilder.Location.Source, rootPropertySourceBuilder.Location.Line, rootPropertySourceBuilder.Location.Column);
                return;
            }

            IList<string> segments = new Collection<string>();
            segments.AddRange(propertySource.PropertyName.Split('.'));
            if (segments.Last() == ItemParameterSource.IndexPropertyName)
                segments.RemoveAt(segments.Count - 1);

            CollectPropertySourceNodes(propertySource, segments, propertySchemaTypeReference, objectSchema);
        }

        private void CollectPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, IEnumerable<string> segments, TypeReference typeReference, ObjectSchema schema)
        {
            TypeReference type = typeReference;
            ObjectSchema objectSchema = schema;
            string currentPath = null;
            int columnOffset = 0;
            foreach (string propertyName in segments)
            {
                currentPath = currentPath == null ? propertyName : $"{currentPath}.{propertyName}";

                if (!CollectPropertyNode(type, objectSchema, propertyName, propertySourceBuilder, base.Logger, columnOffset, out type))
                    return;

                columnOffset += propertyName.Length + 1; // Skip property name + dot
                objectSchema = type is SchemaTypeReference schemaTypeReference ? _schemaRegistry.GetSchema(schemaTypeReference) as ObjectSchema : null;

                if (objectSchema == null)
                    break;
            }
        }

        private static bool CollectPropertyNode(TypeReference type, ObjectSchema objectSchema, string propertyName, ActionParameterPropertySourceBuilder propertySourceBuilder, ILogger logger, int columnOffset, out TypeReference propertyType)
        {
            ObjectSchemaProperty property = objectSchema.Properties.SingleOrDefault(x => x.Name.Value == propertyName);
            if (property != null)
            {
                propertySourceBuilder.Nodes.Add(new ActionParameterPropertySourceNode(objectSchema, property));
                propertyType = property.Type;
                return true;
            }

            int definitionNameOffset = propertySourceBuilder.Definition.Name.Length + 1; // Skip source name + dot
            int column = propertySourceBuilder.Location.Column + definitionNameOffset + columnOffset;
            logger.LogError($"Property '{propertyName}' not found on contract '{type.DisplayName}'", propertySourceBuilder.Location.Source, propertySourceBuilder.Location.Line, column);
            propertyType = null;
            return false;
        }
        #endregion
    }
}