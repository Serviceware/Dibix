using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private const string LockSectionName = "ControllerImport";
        private readonly IDictionary<string, SecurityScheme> _usedSecuritySchemes;
        private readonly SecuritySchemes _securitySchemes;
        private readonly ConfigurationTemplates _templates;
        private readonly IActionTargetDefinitionResolverFacade _actionTargetResolver;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
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
          , ISchemaDefinitionResolver schemaDefinitionResolver
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
            _schemaDefinitionResolver = schemaDefinitionResolver;
            _actionParameterSourceRegistry = actionParameterSourceRegistry;
            _actionParameterConverterRegistry = actionParameterConverterRegistry;
            _lockEntryManager = lockEntryManager;
            Controllers = new Collection<ControllerDefinition>();
            Collect(endpoints.Select(x => x.GetFullPath()));
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json) => ReadControllers(filePath, json);
        #endregion

        #region Private Methods
        private void ReadControllers(string filePath, JObject apis)
        {
            foreach (JProperty apiProperty in apis.Properties())
            {
                if (apiProperty.Name == "$schema")
                    continue;

                ReadController(filePath, apiProperty.Name, (JArray)apiProperty.Value);
            }
        }

        private void ReadController(string filePath, string controllerName, JArray actions)
        {
            ControllerDefinition controller = new ControllerDefinition(controllerName);
            foreach (JToken action in actions) 
                ReadControllerItem(filePath, action, action.Type, controller);

            Controllers.Add(controller);
        }

        private void ReadControllerItem(string filePath, JToken action, JTokenType type, ControllerDefinition controller)
        {
            switch (type)
            {
                case JTokenType.Object:
                    ReadControllerAction(filePath, controller, (JObject)action);
                    break;

                case JTokenType.String:
                    ReadControllerImport(controller, action, filePath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, action.Path);
            }
        }

        private void ReadControllerAction(string filePath, ControllerDefinition controller, JObject action)
        {
            JObject actionMerged = ApplyTemplate(_templates.Default.Action, action);

            // Collect body parameters
            ActionRequestBody requestBody = ReadBody(action, filePath);
            ICollection<string> bodyParameters = GetBodyProperties(requestBody?.Contract, _schemaDefinitionResolver);

            // Collect explicit parameters
            IDictionary<string, ExplicitParameter> explicitParameters = new Dictionary<string, ExplicitParameter>();
            CollectActionParameters(action, filePath, explicitParameters, requestBody);

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

            // Resolve action target, parameters and create action definition
            ActionDefinition actionDefinition = CreateActionDefinition<ActionDefinition>(action, filePath, explicitParameters, pathParameters, bodyParameters);
            if (actionDefinition == null)
                return;

            // Unfortunately we do not have any metadata on reflection targets
            if (!(actionDefinition.Target is ReflectionActionTarget))
            {
                // Validate explicit parameters
                foreach (ExplicitParameter explicitParameter in explicitParameters.Values)
                {
                    base.Logger.LogError($"Parameter '{explicitParameter.Name}' not found on action: {actionDefinition.Target.OperationName}", filePath, explicitParameter.Line, explicitParameter.Column);
                }

                // Validate path parameters
                foreach (PathParameter pathSegment in pathParameters.Values)
                {
                    base.Logger.LogError($"Undefined path parameter: {pathSegment.Name}", filePath, pathSegment.Line, pathSegment.Column);
                }
            }

            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            actionDefinition.Method = method;
            actionDefinition.OperationId = (string)action.Property("operationId")?.Value ?? actionDefinition.Target.OperationName;
            actionDefinition.Description = (string)action.Property("description")?.Value;
            actionDefinition.ChildRoute = childRouteProperty?.Value.ToToken(childRoute, filePath);
            actionDefinition.RequestBody = requestBody;

            if (TryReadFileResponse(action, out ActionFileResponse fileResponse, out IJsonLineInfo fileResponseLocation))
                actionDefinition.SetFileResponse(fileResponse, filePath, fileResponseLocation.LineNumber, fileResponseLocation.LinePosition);

            IJsonLineInfo actionLineInfo = action.GetLineInfo();
            CollectActionResponses(action, actionDefinition, filePath);
            CollectSecuritySchemes(actionMerged, actionDefinition, actionLineInfo, filePath);
            CollectAuthorization(actionMerged, actionDefinition, actionLineInfo, filePath);

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

            IJsonLineInfo lineInfo = action;
            base.Logger.LogError($"Duplicate action registration: {sb}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private static T ApplyTemplate<T>(T source, T target) where T : JContainer
        {
            if (source == null)
                return target;

            T merged = (T)source.DeepClone();
            merged.Merge(target);
            return merged;
        }

        private ActionRequestBody ReadBody(JObject action, string filePath)
        {
            JToken bodyValue = action.Property("body")?.Value;
            if (bodyValue == null) 
                return null;

            return ReadBodyValue(filePath, bodyValue, bodyValue.Type);
        }

        private ActionRequestBody ReadBodyValue(string filePath, JToken value, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    return ReadBodyValue((JObject)value, filePath);

                case JTokenType.String:
                    return ReadBodyValue((JValue)value, filePath);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);
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
                base.Logger.LogError("Body is missing 'contract' property", filePath, valueLocation.LineNumber, valueLocation.LinePosition);
                return null;
            }

            TypeReference contract = ResolveType(contractName, filePath);
            return new ActionRequestBody(mediaType, contract, binder);
        }
        private ActionRequestBody ReadBodyValue(JValue value, string filePath)
        {
            TypeReference contract = ResolveType(value, filePath);
            return new ActionRequestBody(contract);
        }

        private static ICollection<string> GetBodyProperties(TypeReference bodyContract, ISchemaDefinitionResolver schemaDefinitionResolver)
        {
            HashSet<string> properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bodyContract is SchemaTypeReference schemaTypeReference)
            {
                SchemaDefinition schema = schemaDefinitionResolver.Resolve(schemaTypeReference);
                if (schema is ObjectSchema objectSchema)
                    properties.AddRange(objectSchema.Properties.Select(x => x.Name.Value));
            }
            return properties;
        }

        private void ReadControllerImport(ControllerDefinition controller, JToken value, string filePath)
        {
            string typeName = (string)value;
            if (!_lockEntryManager.HasEntry(LockSectionName, typeName))
            {
                IJsonLineInfo lineInfo = value.GetLineInfo();
                base.Logger.LogError("Controller imports are not supported anymore", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                return;
            }

            controller.ControllerImports.Add(typeName);
        }

        private void CollectActionParameters(JObject action, string filePath, IDictionary<string, ExplicitParameter> target, ActionRequestBody requestBody)
        {
            JObject mappings = (JObject)action.Property("params")?.Value;
            if (mappings == null)
                return;

            foreach (JProperty property in mappings.Properties())
            {
                if (!TryReadSource(property, filePath, requestBody: requestBody, rootPropertySourceBuilder: null, out ActionParameterSourceBuilder parameterSourceBuilder))
                {
                    parameterSourceBuilder = CollectActionParameterSource(filePath, requestBody, property, property.Value.Type);
                }

                ExplicitParameter parameter = new ExplicitParameter(property, parameterSourceBuilder);
                target.Add(property.Name, parameter);
            }
        }

        private ActionParameterSourceBuilder CollectActionParameterSource(string filePath, ActionRequestBody requestBody, JProperty property, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object: return ReadComplexActionParameter(property.Name, (JObject)property.Value, filePath, requestBody);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }

        private bool TryReadSource(JProperty property, string filePath, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootPropertySourceBuilder, out ActionParameterSourceBuilder parameterSourceBuilder)
        {
            switch (property.Value.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Null:
                    parameterSourceBuilder = new ActionParameterConstantSourceBuilder((JValue)property.Value, filePath, _schemaDefinitionResolver, Logger);
                    return true;

                case JTokenType.String:
                    string stringValue = (string)property.Value;
                    if (stringValue != null && stringValue.Contains('.'))
                    {
                        parameterSourceBuilder = ReadPropertySource((JValue)property.Value, filePath, requestBody, rootPropertySourceBuilder);
                    }
                    else
                    {
                        parameterSourceBuilder = new ActionParameterConstantSourceBuilder((JValue)property.Value, filePath, _schemaDefinitionResolver, Logger);
                    }
                    return true;

                default:
                    parameterSourceBuilder = null;
                    return false;
            }
        }

        private ActionParameterPropertySourceBuilder ReadPropertySource(JValue value, string filePath, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootParameterSourceBuilder)
        {
            string[] parts = ((string)value.Value).Split(new[] { '.' }, 2);
            string sourceName = parts[0];
            string propertyName = parts[1];
            IJsonLineInfo valueLocation = value.GetLineInfo();

            if (!_actionParameterSourceRegistry.TryGetDefinition(sourceName, out ActionParameterSourceDefinition definition))
            {
                base.Logger.LogError($"Unknown property source '{sourceName}'", filePath, valueLocation.LineNumber, valueLocation.LinePosition);
            }

            ActionParameterPropertySourceBuilder propertySourceBuilder = new ActionParameterPropertySourceBuilder(definition, propertyName, filePath, valueLocation.LineNumber, valueLocation.LinePosition);
            CollectPropertySourceNodes(propertySourceBuilder, requestBody, rootParameterSourceBuilder);
            return propertySourceBuilder;
        }

        private ActionParameterSourceBuilder ReadComplexActionParameter(string parameterName, JObject container, string filePath, ActionRequestBody requestBody)
        {
            JProperty bodyConverterProperty = container.Property("convertFromBody");
            if (bodyConverterProperty != null)
            {
                return ReadBodyActionParameter(bodyConverterProperty);
            }

            JProperty sourceProperty = container.Property("source");
            if (sourceProperty != null)
            {
                return ReadPropertyActionParameter(parameterName, container, sourceProperty, filePath, requestBody);
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private ActionParameterSourceBuilder ReadPropertyActionParameter(string parameterName, JObject container, JProperty sourceProperty, string filePath, ActionRequestBody requestBody)
        {
            ActionParameterPropertySourceBuilder propertySourceBuilder = ReadPropertySource((JValue)sourceProperty.Value, filePath, requestBody, rootParameterSourceBuilder: null);

            JProperty itemsProperty = container.Property("items");
            if (itemsProperty != null)
            {
                JObject itemsObject = (JObject)itemsProperty.Value;
                foreach (JProperty itemProperty in itemsObject.Properties())
                {
                    if (TryReadSource(itemProperty, filePath, requestBody: requestBody, rootPropertySourceBuilder: propertySourceBuilder, out ActionParameterSourceBuilder itemPropertySourceBuilder))
                    {
                        IJsonLineInfo lineInfo = itemProperty.GetLineInfo();
                        propertySourceBuilder.ItemSources.Add(new ActionParameterItemSourceBuilder(itemProperty.Name, itemPropertySourceBuilder, filePath, lineInfo.LineNumber, lineInfo.LinePosition, _schemaDefinitionResolver, Logger));
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
                    IJsonLineInfo converterLocation = converter.GetLineInfo();
                    base.Logger.LogError($"Unknown property converter '{converterName}'", filePath, converterLocation.LineNumber, converterLocation.LinePosition);
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

        private T CreateActionDefinition<T>(JObject action, string filePath, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters) where T : ActionTargetDefinition, new()
        {
            JValue targetValue = (JValue)action.Property("target").Value;
            IJsonLineInfo lineInfo = targetValue.GetLineInfo();
            T actionDefinition = _actionTargetResolver.Resolve<T>(targetName: (string)targetValue, filePath, lineInfo.LineNumber, lineInfo.LinePosition, explicitParameters, pathParameters, bodyParameters);
            return actionDefinition;
        }

        private void CollectActionResponses(JObject actionJson, ActionDefinition actionDefinition, string filePath)
        {
            JObject responses = (JObject)actionJson.Property("responses")?.Value;
            if (responses == null)
                return;

            foreach (JProperty property in responses.Properties()) 
                CollectActionResponse(property, property.Value.Type, actionDefinition, filePath);
        }

        private void CollectActionResponse(JProperty property, JTokenType type, ActionTargetDefinition actionTargetDefinition, string filePath)
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
                    response.ResultType = ResolveType((JValue)property.Value, filePath);
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
                            response.ResultType = ResolveType(typeNameValue, filePath);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }

        private void CollectSecuritySchemes(JObject actionJson, ActionDefinition actionDefinition, IJsonLineInfo actionLineInfo, string filePath)
        {
            const string propertyName = "securitySchemes";
            JProperty property = actionJson.Property(propertyName);
            if (property == null)
            {
                base.Logger.LogError($"Missing required property '{propertyName}'", filePath, actionLineInfo.LineNumber, actionLineInfo.LinePosition);
                return;
            }

            IEnumerable<SecurityScheme> schemes = CollectSecurityScheme(property, property.Value.Type, filePath);
            actionDefinition.SecuritySchemes.Requirements.AddRange(schemes.Select(x => new SecuritySchemeRequirement(x)));
        }

        private IEnumerable<SecurityScheme> CollectSecurityScheme(JProperty property, JTokenType type, string filePath)
        {
            switch (type)
            {
                case JTokenType.String:
                    foreach (SecurityScheme securityScheme in CollectSecurityScheme(property.Value, property.Value.Type, filePath))
                        yield return securityScheme;

                    break;

                case JTokenType.Array:
                    JArray array = (JArray)property.Value;
                    foreach (SecurityScheme securityScheme in array.SelectMany(x => CollectSecurityScheme(x, x.Type, filePath)))
                        yield return securityScheme;

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }
        private IEnumerable<SecurityScheme> CollectSecurityScheme(JToken value, JTokenType type, string filePath)
        {
            if (type != JTokenType.String)
                throw new ArgumentOutOfRangeException(nameof(type), type, value.Path);

            string name = (string)value;
            if (!_usedSecuritySchemes.TryGetValue(name, out SecurityScheme scheme))
            {
                if (!_securitySchemes.TryFindSecurityScheme(name, out scheme))
                {
                    IJsonLineInfo lineInfo = value;
                    string possibleValues = String.Join(", ", _securitySchemes.Schemes.Select(x => x.Name));
                    base.Logger.LogError($"Unknown authorization scheme '{name}'. Possible values are: {possibleValues}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                    yield break;
                }
                _usedSecuritySchemes.Add(name, scheme);
            }
            yield return scheme;
        }

        private void CollectAuthorization(JObject actionJson, ActionDefinition actionDefinition, IJsonLineInfo actionLineInfo, string filePath)
        {
            const string propertyName = "authorization";
            JProperty property = actionJson.Property(propertyName);
            if (property == null)
            {
                base.Logger.LogError($"Missing required property '{propertyName}'", filePath, actionLineInfo.LineNumber, actionLineInfo.LinePosition);
                return;
            }

            CollectAuthorization(property, property.Value.Type, actionDefinition, filePath);
        }
        private void CollectAuthorization(JProperty property, JTokenType type, ActionDefinition actionDefinition, string filePath)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JObject authorizationValue = (JObject)property.Value;
                    JProperty templateProperty = authorizationValue.Property("name");
                    CollectAuthorization(templateProperty, authorizationValue, actionDefinition, filePath);
                    break;

                case JTokenType.String when (string)property.Value == "none":
                    break;

                case JTokenType.String:
                    CollectAuthorization(templateProperty: property, authorizationValue: new JObject(), actionDefinition, filePath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, property.Value.Path);
            }
        }
        private void CollectAuthorization(JProperty templateProperty, JObject authorizationValue, ActionDefinition actionDefinition, string filePath)
        {
            JObject authorization = authorizationValue;

            if (templateProperty != null
             && (authorization = ApplyAuthorizationTemplate(templateProperty, filePath, authorizationValue)) == null) // In case of error that has been previously logged
                return;

            IDictionary<string, ExplicitParameter> explicitParameters = new Dictionary<string, ExplicitParameter>();
            CollectActionParameters(authorization, filePath, explicitParameters, requestBody: null);
            IDictionary<string, PathParameter> pathParameters = new Dictionary<string, PathParameter>();
            ICollection<string> bodyParameters = new Collection<string>();
            actionDefinition.Authorization = CreateActionDefinition<AuthorizationBehavior>(authorization, filePath, explicitParameters, pathParameters, bodyParameters);
        }

        private JObject ApplyAuthorizationTemplate(JProperty templateNameProperty, string filePath, JObject authorizationTemplateReference)
        {
            string templateName = (string)templateNameProperty.Value;
            if (!_templates.Authorization.TryGetTemplate(templateName, out ConfigurationAuthorizationTemplate template))
            {
                IJsonLineInfo templateNameLineInfo = templateNameProperty.Value.GetLineInfo();
                Logger.LogError($"Unknown authorization template '{templateName}'", filePath, templateNameLineInfo.LineNumber, templateNameLineInfo.LinePosition);
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

            JObject mergedAuthorization = ApplyTemplate(template.Content, resolvedAuthorization);
            return mergedAuthorization;
        }

        private TypeReference ResolveType(JValue typeNameValue, string filePath)
        {
            string typeName = (string)typeNameValue;
            IJsonLineInfo typeNameLocation = typeNameValue.GetLineInfo();
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            TypeReference type = _typeResolver.ResolveType(typeName, null, filePath, typeNameLocation.LineNumber, typeNameLocation.LinePosition, isEnumerable);
            return type;
        }

        private void CollectPropertySourceNodes(ActionParameterPropertySourceBuilder propertySourceBuilder, ActionRequestBody requestBody, ActionParameterPropertySourceBuilder rootPropertySourceBuilder)
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
                base.Logger.LogError("Must specify a body contract on the endpoint action when using BODY property source", propertySourceBuilder.FilePath, propertySourceBuilder.Line, propertySourceBuilder.Column);
                return;
            }

            // Only traverse, if the body is an object contract
            TypeReference type = requestBody.Contract;
            if (!(type is SchemaTypeReference bodySchemaTypeReference))
                return;

            if (!(_schemaDefinitionResolver.Resolve(bodySchemaTypeReference) is ObjectSchema objectSchema))
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
            if (!(type is SchemaTypeReference propertySchemaTypeReference))
            {
                base.Logger.LogError($"Unexpected type '{type?.GetType()}' for property '{rootPropertySourceBuilder.PropertyName}'. Only object schemas can be used for UDT item mappings.", rootPropertySourceBuilder.FilePath, rootPropertySourceBuilder.Line, rootPropertySourceBuilder.Column);
                return;
            }

            SchemaDefinition propertySchema = _schemaDefinitionResolver.Resolve(propertySchemaTypeReference);
            if (!(propertySchema is ObjectSchema objectSchema))
            {
                base.Logger.LogError($"Unexpected type '{propertySchema?.GetType()}' for property '{rootPropertySourceBuilder.PropertyName}'. Only object schemas can be used for UDT item mappings.", rootPropertySourceBuilder.FilePath, rootPropertySourceBuilder.Line, rootPropertySourceBuilder.Column);
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
                objectSchema = type is SchemaTypeReference schemaTypeReference ? _schemaDefinitionResolver.Resolve(schemaTypeReference) as ObjectSchema : null;
            }
        }

        private static bool CollectPropertyNode(TypeReference type, ObjectSchema objectSchema, string propertyName, ActionParameterPropertySourceBuilder propertySourceBuilder, ILogger logger, int columnOffset, out TypeReference propertyType)
        {
            ObjectSchemaProperty property = objectSchema?.Properties.SingleOrDefault(x => x.Name.Value == propertyName);
            if (property != null)
            {
                propertySourceBuilder.Nodes.Add(new ActionParameterPropertySourceNode(objectSchema, property));
                propertyType = property.Type;
                return true;
            }

            int definitionNameOffset = propertySourceBuilder.Definition.Name.Length + 1; // Skip source name + dot
            int column = propertySourceBuilder.Column + definitionNameOffset + columnOffset;
            logger.LogError($"Property '{propertyName}' not found on contract '{type.DisplayName}'", propertySourceBuilder.FilePath, propertySourceBuilder.Line, column);
            propertyType = null;
            return false;
        }
        #endregion
    }
}