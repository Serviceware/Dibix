using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AssemblyName = System.Reflection.AssemblyName;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private readonly string _projectName;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _outputName;
        private readonly ICollection<SqlStatementInfo> _statements;
        private readonly IDictionary<string, SecurityScheme> _securitySchemeMap;
        private readonly ICollection<string> _defaultSecuritySchemes;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly AssemblyResolver _assemblyResolver;
        #endregion

        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        public ICollection<SecurityScheme> SecuritySchemes { get; }
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider
        (
            string projectName
          , string productName
          , string areaName
          , string outputName
          , ICollection<SqlStatementInfo> statements
          , IEnumerable<string> endpoints
          , ICollection<string> defaultSecuritySchemes
          , IDictionary<string, SecurityScheme> securitySchemeMap
          , ITypeResolverFacade typeResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ISchemaRegistry schemaRegistry
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
        ) : base(fileSystemProvider, logger)
        {
            this._projectName = projectName;
            this._productName = productName;
            this._areaName = areaName;
            this._outputName = outputName;
            this._statements = statements;
            this._securitySchemeMap = securitySchemeMap;
            this._defaultSecuritySchemes = defaultSecuritySchemes;
            this._typeResolver = typeResolver;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._schemaRegistry = schemaRegistry;
            this._assemblyResolver = referencedAssemblyInspector;
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
                        ReadControllerImport(controller, (string)action);
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
            CollectActionParameters(action, explicitParameters);

            // Collect path parameters
            IDictionary<string, Group> pathParameters = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);
            JProperty childRouteProperty = action.Property("childRoute");
            string childRoute = (string)childRouteProperty?.Value;
            if (childRouteProperty != null)
            {
                IEnumerable<Group> pathSegments = HttpParameterUtility.ExtractPathParameters(childRoute);
                foreach (Group pathSegment in pathSegments) 
                    pathParameters.Add(pathSegment.Value, pathSegment);
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
                    IJsonLineInfo propertyLocation = explicitParameter.Property;
                    base.Logger.LogError(null, $"Parameter '{explicitParameter.Property.Name}' not found on action: {actionDefinition.Target.OperationName}", filePath, propertyLocation.LineNumber, explicitParameter.Property.GetCorrectLinePosition());
                }

                // Validate path parameters
                foreach (Group pathSegment in pathParameters.Values)
                {
                    JValue childRouteValue = (JValue)childRouteProperty.Value;
                    IJsonLineInfo childRouteValueLocation = childRouteProperty.Value;
                    int matchIndex = pathSegment.Index - 1;
                    base.Logger.LogError(null, $"Undefined path parameter: {pathSegment}", filePath, childRouteValueLocation.LineNumber, childRouteValue.GetCorrectLinePosition() + matchIndex);
                }
            }

            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            actionDefinition.Method = method;
            actionDefinition.Description = (string)action.Property("description")?.Value;
            actionDefinition.ChildRoute = childRoute;
            actionDefinition.RequestBody = requestBody;
            actionDefinition.FileResponse = ReadFileResponse(action);

            if (actionDefinition.FileResponse != null)
            {
                actionDefinition.Responses[HttpStatusCode.OK] = new ActionResponse(HttpStatusCode.OK, actionDefinition.FileResponse.MediaType, CreateStreamTypeReference());
                actionDefinition.Responses[HttpStatusCode.NotFound] = new ActionResponse(HttpStatusCode.NotFound);
            }

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
            string mediaType = (string)value.Property("mediaType")?.Value;
            string binder = (string)value.Property("binder")?.Value;

            if (mediaType != null && mediaType != HttpMediaType.Default)
                return new ActionRequestBody(mediaType, CreateStreamTypeReference());

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

        private static void ReadControllerImport(ControllerDefinition controller, string typeName)
        {
            controller.ControllerImports.Add(typeName);
        }

        private static void CollectActionParameters(JObject action, IDictionary<string, ExplicitParameter> target)
        {
            JObject mappings = (JObject)action.Property("params")?.Value;
            if (mappings == null)
                return;

            foreach (JProperty property in mappings.Properties())
            {
                if (!TryReadSource(property, out ActionParameterSource source))
                {
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            source = ReadComplexActionParameter(property.Name, (JObject)property.Value);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Value.Path), property.Value.Type, null);
                    }
                }

                ExplicitParameter parameter = new ExplicitParameter(property, source);
                target.Add(property.Name, parameter);
            }
        }

        private static bool TryReadSource(JProperty property, out ActionParameterSource source)
        {
            switch (property.Value.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Null:
                    source = ReadConstantSource((JValue)property.Value);
                    return true;

                case JTokenType.String:
                    source = ReadPropertySource((JValue)property.Value);
                    return true;

                default:
                    source = null;
                    return false;
            }
        }

        private static ActionParameterSource ReadConstantSource(JValue value) => new ActionParameterConstantSource(value.Value);

        private static ActionParameterPropertySource ReadPropertySource(JValue value)
        {
            string[] parts = ((string)value.Value).Split(new[] { '.' }, 2);
            ActionParameterPropertySource propertySource = new ActionParameterPropertySource(parts[0], parts[1]);
            return propertySource;
        }

        private static ActionParameterSource ReadComplexActionParameter(string parameterName, JObject container)
        {
            JProperty bodyConverterProperty = container.Property("convertFromBody");
            if (bodyConverterProperty != null)
            {
                return ReadBodyActionParameter(bodyConverterProperty);
            }

            JProperty sourceProperty = container.Property("source");
            if (sourceProperty != null)
            {
                return ReadPropertyActionParameter(parameterName, container, sourceProperty);
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static ActionParameterSource ReadPropertyActionParameter(string parameterName, JObject container, JProperty sourceProperty)
        {
            ActionParameterPropertySource propertySource = ReadPropertySource((JValue)sourceProperty.Value);

            JProperty itemsProperty = container.Property("items");
            if (itemsProperty != null)
            {
                JObject itemsObject = (JObject)itemsProperty.Value;
                foreach (JProperty itemProperty in itemsObject.Properties())
                {
                    if (TryReadSource(itemProperty, out ActionParameterSource itemPropertySource))
                        propertySource.ItemSources.Add(itemProperty.Name, itemPropertySource);
                }
                return propertySource;
            }

            JProperty converterProperty = container.Property("converter");
            if (converterProperty != null)
            {
                propertySource.Converter = (string)((JValue)converterProperty.Value).Value;
                return propertySource;
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static ActionParameterBodySource ReadBodyActionParameter(JProperty bodyConverterProperty)
        {
            string bodyConverterTypeName = (string)((JValue)bodyConverterProperty.Value).Value;
            return new ActionParameterBodySource(bodyConverterTypeName);
        }

        private static ActionFileResponse ReadFileResponse(JObject action)
        {
            JObject fileResponseValue = (JObject)action.Property("fileResponse")?.Value;
            if (fileResponseValue == null)
                return null;

            JProperty mediaTypeProperty = fileResponseValue.Property("mediaType");
            if (mediaTypeProperty == null)
                throw new InvalidOperationException("Missing required property fileResponse.mediaType");

            JProperty cacheProperty = fileResponseValue.Property("cache");

            string mediaType = (string)mediaTypeProperty.Value;
            ActionFileResponse fileResponse = new ActionFileResponse(mediaType);

            if (cacheProperty != null)
                fileResponse.Cache = (bool)cacheProperty.Value;

            return fileResponse;
        }

        private ActionDefinition CreateActionDefinition(JObject action, string filePath, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            JValue targetValue = (JValue)action.Property("target").Value;
            IJsonLineInfo lineInfo = targetValue;
            ActionDefinition actionDefinition = this.CreateActionDefinition((string)targetValue, filePath, lineInfo.LineNumber, targetValue.GetCorrectLinePosition(), explicitParameters, pathParameters, bodyParameters);
            return actionDefinition;
        }
        private ActionDefinition CreateActionDefinition(string target, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            // 1. Target is a reflection target within a foreign assembly
            if (this.TryGetExternalActionTarget(target, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out ActionDefinition actionDefinition))
                return actionDefinition;

            // Use explicit namespace if it can be extracted
            int statementNameIndex = target.LastIndexOf('.');
            string @namespace = statementNameIndex >= 0 ? target.Substring(0, statementNameIndex) : null;

            // Detect absolute namespace if it is prefixed with the product name
            // i.E.: Data.Runtime is a relative namespace
            bool isAbsolute = target.StartsWith($"{this._productName}.", StringComparison.Ordinal);
            string normalizedNamespace = @namespace;
            if (!isAbsolute)
                normalizedNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.Data, @namespace);

            string typeName = $"{normalizedNamespace}.{this._outputName}";
            string methodName = target.Substring(statementNameIndex + 1);

            // 2. Target is a SQL statement within the current project
            SqlStatementInfo statement = this._statements.FirstOrDefault(x => x.Namespace == normalizedNamespace && x.Name == methodName);
            if (statement != null)
            {
                ActionDefinitionTarget actionTarget = new LocalActionTarget(statement, this._outputName);
                actionDefinition = new ActionDefinition(actionTarget);
                ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
                foreach (SqlQueryParameter parameter in statement.Parameters)
                {
                    this.CollectActionParameter
                    (
                        parameter.Name
                      , parameter.Type
                      , parameter.DefaultValue
                      , parameter.IsOutput
                      , target
                      , filePath
                      , line
                      , column
                      , parameterRegistry
                      , explicitParameters
                      , pathParameters
                      , bodyParameters
                    );
                }

                foreach (ErrorResponse errorResponse in statement.ErrorResponses)
                    RegisterErrorResponse(actionDefinition, errorResponse.StatusCode, errorResponse.ErrorCode, errorResponse.ErrorDescription);

                actionDefinition.DefaultResponseType = statement.ResultType;

                return actionDefinition;
            }

            // Relative namespaces can not be resolved in neighbor projects
            if (!isAbsolute)
            {
                base.Logger.LogError(null, $@"Could not find action target: {target}
Tried: {normalizedNamespace}.{methodName}", filePath, line, column);
                return null;
            }

            // 3. Target 'could' be a compiled method in a neighbour project
            if (!this.TryGetNeighborActionTarget(target, methodName, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out actionDefinition))
            {
                base.Logger.LogError(null, $"Could not find a method named '{methodName}' on database accessor type '{typeName}'", filePath, line, column);
                return null;
            }
            return actionDefinition;
        }

        private bool TryGetExternalActionTarget(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters, out ActionDefinition actionDefinition)
        {
            string[] parts = targetName.Split(',');
            if (parts.Length != 2)
            {
                actionDefinition = null;
                return false;
            }
            
            string assemblyName = parts[1];
            int methodNameIndex = parts[0].LastIndexOf('.');
            string typeName = parts[0].Substring(0, methodNameIndex);
            string methodName = parts[0].Substring(methodNameIndex + 1, parts[0].Length - methodNameIndex - 1);

            /*
            if (!this._assemblyResolver.TryGetAssembly(assemblyName, out Assembly assembly))
            {
                base.Logger.LogError(null, $"Could not locate assembly: {assemblyName}", filePath, line, column + parts[0].Length + 1);
                actionDefinition = null;
                return true;
            }

            Type type = assembly.GetType(typeName, true);
            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
            {
                base.Logger.LogError(null, $"Could not find method: {methodName} on {typeName}", filePath, line, column + methodNameIndex + 1);
                actionDefinition = null;
                return true;
            }

            actionDefinition = ReflectionOnlyTypeInspector.Inspect(() => this.CreateActionDefinition(targetName, assemblyName, method, filePath, line, column, explicitParameters, pathParameters, bodyParameters));
            */

            actionDefinition = new ActionDefinition(new ReflectionActionTarget(assemblyName, typeName, methodName, isAsync: false, hasRefParameters: false));
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
            foreach (ExplicitParameter parameter in explicitParameters.Values)
            {
                string apiParameterName = parameter.Property.Name;
                string internalParameterName = apiParameterName;
                ActionParameterLocation location = ResolveParameterLocationFromSource(parameter.Source, ref apiParameterName);
                if (location == ActionParameterLocation.Path)
                    pathParameters.Remove(apiParameterName);

                bool isRequired = IsParameterRequired(type: null, location, defaultValue: null, this._schemaRegistry);
                parameterRegistry.Add(new ActionParameter(apiParameterName, internalParameterName, type: null, location, isRequired, defaultValue: null, parameter.Source));
            }

            foreach (string pathParameter in pathParameters.Keys)
            {
                TypeReference typeReference = new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false);
                ActionParameterLocation location = ActionParameterLocation.Path;
                bool isRequired = IsParameterRequired(type: null, location, defaultValue: null, this._schemaRegistry);
                ActionParameter parameter = new ActionParameter(pathParameter, pathParameter, typeReference, location, isRequired, defaultValue: null, source: null);
                actionDefinition.Parameters.Add(parameter);
            }

            return true;
        }

        private bool TryGetNeighborActionTarget(string targetName, string methodName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters, out ActionDefinition actionDefinition)
        {
            actionDefinition = this._referencedAssemblyInspector.Inspect(referencedAssemblies =>
            {
                var query = from assembly in referencedAssemblies
                            where assembly.IsArtifactAssembly()
                            from type in assembly.GetTypes()
                            where type.IsDatabaseAccessor()
                            from method in type.GetMethods()
                            where method.Name == methodName
                               || method.Name == $"{methodName}Async"
                            select this.CreateActionDefinition(targetName, assemblyName: null, method, filePath, line, column, explicitParameters, pathParameters, bodyParameters);

                return query.FirstOrDefault();
            });

            return actionDefinition != null;
        }

        private ActionDefinition CreateActionDefinition(string targetName, string assemblyName, MethodInfo method, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            string operationName = method.Name;
            bool isReflectionTarget = assemblyName != null;
            Type returnType = this.CollectReturnType(method, isReflectionTarget);
            bool isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
            bool hasRefParameters = method.GetParameters().Any(x => x.ParameterType.IsByRef);

            if (isAsync)
            {
                returnType = returnType.GenericTypeArguments[0];
                const string asyncSuffix = "Async";
                if (operationName.EndsWith(asyncSuffix, StringComparison.Ordinal))
                    operationName = operationName.Remove(operationName.Length - asyncSuffix.Length);
            }

            TypeReference resultType = null;
            if (returnType != typeof(void))
                resultType = ReflectionTypeResolver.ResolveType(returnType, filePath, line, column, this._schemaRegistry, base.Logger);

            NeighborActionTarget target;
            if (isReflectionTarget)
                target = new ReflectionActionTarget(assemblyName, method.DeclaringType.FullName, operationName, isAsync, hasRefParameters);
            else
                target = new NeighborActionTarget(method.DeclaringType.FullName, operationName, isAsync, hasRefParameters);

            ActionDefinition actionDefinition = new ActionDefinition(target);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
            method.CollectErrorResponses((statusCode, errorCode, errorDescription) => RegisterErrorResponse(actionDefinition, statusCode, errorCode, errorDescription));
            actionDefinition.DefaultResponseType = resultType;

            IEnumerable<ParameterInfo> parameters = this.CollectReflectionInfo(() => method.GetExternalParameters(isAsync), isReflectionTarget, Enumerable.Empty<ParameterInfo>);
            foreach (ParameterInfo parameter in parameters)
            {
                string parameterName = parameter.Name;
                TypeReference parameterType = ReflectionTypeResolver.ResolveType(parameter.ParameterType, filePath, line, column, this._schemaRegistry, base.Logger);

                // ParameterInfo.HasDefaultValue/DefaultValue => It is illegal to reflect on the custom attributes of a Type loaded via ReflectionOnlyGetType (see Assembly.ReflectionOnly) -- use CustomAttributeData instead
                DefaultValue defaultValue = parameter.RawDefaultValue != DBNull.Value ? new DefaultValue(parameter.RawDefaultValue, filePath, line, column) : null;
                bool isOutParameter = parameter.IsOut;

                this.CollectActionParameter
                (
                    parameterName
                  , parameterType
                  , defaultValue
                  , isOutParameter
                  , targetName
                  , filePath
                  , line
                  , column
                  , parameterRegistry
                  , explicitParameters
                  , pathParameters
                  , bodyParameters
                );
            }

            return actionDefinition;
        }

        private Type CollectReturnType(MethodInfo method, bool isReflectionTarget)
        {
            Type returnType = this.CollectReflectionInfo(() => method.ReturnType, isReflectionTarget, () => typeof(void));
            if (isReflectionTarget && returnType.FullName == typeof(HttpResponseMessage).FullName)
                return typeof(void);

            return returnType;
        }

        private T CollectReflectionInfo<T>(Func<T> valueResolver, bool isReflectionTarget, Func<T> fallbackValueResolver)
        {
            if (!isReflectionTarget)
                return valueResolver();

            try
            {
                return valueResolver();
            }
            catch (FileNotFoundException exception)
            {
                AssemblyName assemblyName = new AssemblyName(exception.FileName);
                if (assemblyName.Name == this._projectName)
                    return fallbackValueResolver();

                throw;
            }
        }

        private void CollectActionParameter
        (
            string parameterName
          , TypeReference parameterType
          , DefaultValue defaultValue
          , bool isOutParameter
          , string actionName
          , string filePath
          , int line
          , int column
          , ActionParameterRegistry parameterRegistry
          , IDictionary<string, ExplicitParameter> explicitParameters
          , IDictionary<string, Group> pathParameters
          , ICollection<string> bodyParameters
        )
        {
            if (isOutParameter)
            {
                //base.Logger.LogError(null, $"Output parameters are not supported in endpoints: {actionName}", filePath, line, column);

                // We don't support out parameters in REST APIs, so we assume that this method is used as both backend accessor and REST API.
                // Therefore we just skip it
                return;
            }

            ActionParameter actionParameter = this.CreateActionParameter(parameterName, parameterType, defaultValue, explicitParameters, pathParameters, bodyParameters);
            parameterRegistry.Add(actionParameter);
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
            IJsonLineInfo typeNameLocation = typeNameValue;
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            TypeReference type = this._typeResolver.ResolveType(typeName, null, filePath, typeNameLocation.LineNumber, typeNameValue.GetCorrectLinePosition(), isEnumerable);
            return type;
        }

        private static void RegisterErrorResponse(ActionDefinition actionDefinition, int statusCode, int errorCode, string errorDescription)
        {
            HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
            if (!actionDefinition.Responses.TryGetValue(httpStatusCode, out ActionResponse response))
            {
                response = new ActionResponse(httpStatusCode);
                actionDefinition.Responses.Add(httpStatusCode, response);
            }
            response.Errors.Add(new ErrorDescription(errorCode, errorDescription));
        }

        private ActionParameter CreateActionParameter(string name, TypeReference type, DefaultValue defaultValue, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            ActionParameterLocation location = ActionParameterLocation.NonUser;
            string apiParameterName = name;
            string internalParameterName = name;

            if (explicitParameters.TryGetValue(name, out ExplicitParameter explicitParameter))
            {
                explicitParameters.Remove(name);

                if (explicitParameter.Source is ActionParameterPropertySource propertySource)
                {
                    apiParameterName = propertySource.PropertyName.Split('.')[0];
                    if (IsUserParameter(propertySource.SourceName, propertySource.PropertyName, ref location, ref apiParameterName))
                    {
                        if (location == ActionParameterLocation.Path)
                            pathParameters.Remove(propertySource.PropertyName);
                    }
                }

                // See CodeGenerationTaskTests.Endpoints
                // Here the user parameter is expected, but overwritten internally
                // Not sure if this really a use case
                // 
                // "childRoute": "{password}/Fixed",
                // "params": {
                //    "password": null
                // }
                if (pathParameters.Remove(name))
                    location = ActionParameterLocation.Path;
            }
            else if (pathParameters.TryGetValue(name, out Group pathSegment))
            {
                apiParameterName = pathSegment.Value;
                location = ActionParameterLocation.Path;
                pathParameters.Remove(name);
            }
            else if (bodyParameters.Contains(name))
            {
                location = ActionParameterLocation.Body;
            }
            else
            {
                location = ActionParameterLocation.Query;
            }

            bool isRequired = IsParameterRequired(type, location, defaultValue, this._schemaRegistry);
            return new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, defaultValue, explicitParameter?.Source);
        }

        private static ActionParameterLocation ResolveParameterLocationFromSource(ActionParameterSource parameterSource, ref string apiParameterName)
        {
            switch (parameterSource)
            {
                case ActionParameterBodySource _: return ActionParameterLocation.Body;

                case ActionParameterConstantSource _: return ActionParameterLocation.NonUser;

                case ActionParameterPropertySource actionParameterPropertySource:
                    ActionParameterLocation location = ActionParameterLocation.NonUser;
                    apiParameterName = actionParameterPropertySource.PropertyName.Split('.')[0];
                    IsUserParameter(actionParameterPropertySource.SourceName, actionParameterPropertySource.PropertyName, ref location, ref apiParameterName);
                    return location;

                default: throw new ArgumentOutOfRangeException(nameof(parameterSource), parameterSource, null);
            }
        }

        private static bool IsUserParameter(string sourceName, string propertyName, ref ActionParameterLocation location, ref string apiParameterName)
        {
            switch (sourceName)
            {
                case "QUERY":
                    location = ActionParameterLocation.Query;
                    return true;

                case "PATH":
                    location = ActionParameterLocation.Path;
                    return true;

                case "BODY":
                    location = ActionParameterLocation.Body;
                    return true;

                case "HEADER":
                    location = ActionParameterLocation.Header;
                    return true;

                case "REQUEST" when propertyName == "Language":
                    location = ActionParameterLocation.Header;
                    apiParameterName = "Accept-Language";
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsParameterRequired(TypeReference type, ActionParameterLocation location, DefaultValue defaultValue, ISchemaRegistry schemaRegistry)
        {
            switch (location)
            {
                case ActionParameterLocation.Query:
                    return defaultValue == null && Equals(type?.IsUserDefinedType(schemaRegistry), false);

                case ActionParameterLocation.Header:
                    return defaultValue == null;

                default:
                    return true;
            }
        }

        private static TypeReference CreateStreamTypeReference() => new PrimitiveTypeReference(PrimitiveType.Stream, isNullable: false, isEnumerable: false);
        #endregion

        #region Nested Types
        private sealed class ExplicitParameter
        {
            public JProperty Property { get; }
            public ActionParameterSource Source { get; }

            public ExplicitParameter(JProperty property, ActionParameterSource source)
            {
                this.Property = property;
                this.Source = source;
            }
        }

        private sealed class ActionParameterRegistry
        {
            private readonly ActionDefinition _actionDefinition;
            private readonly IDictionary<string, int> _pathSegmentIndexMap;
            private int _previousPathSegmentIndex;
            private int _previousPathParameterIndex = -1;

            public ActionParameterRegistry(ActionDefinition actionDefinition, IDictionary<string, Group> pathParameters)
            {
                this._actionDefinition = actionDefinition;
                this._pathSegmentIndexMap = pathParameters.OrderBy(x => x.Value.Index)
                                                          .Select((x, i) => new
                                                          {
                                                              Index = i,
                                                              Key = x.Key
                                                          })
                                                          .ToDictionary(x => x.Key, x => x.Index);
            }

            public void Add(ActionParameter actionParameter)
            {
                int index = this._previousPathParameterIndex + 1;
                if (actionParameter.Location == ActionParameterLocation.Path)
                {
                    // Restore original path parameter order from route template
                    int currentPathSegmentIndex = this._pathSegmentIndexMap[actionParameter.ApiParameterName];
                    bool insertBefore = this._previousPathSegmentIndex > currentPathSegmentIndex;
                    this._previousPathSegmentIndex = currentPathSegmentIndex;
                    if (insertBefore)
                    {
                        index = this._previousPathParameterIndex;
                        this._previousPathParameterIndex = index;
                    }
                    this._previousPathParameterIndex++;

                }
                else
                    index = this._actionDefinition.Parameters.Count;

                this._actionDefinition.Parameters.Insert(index, actionParameter);
            }
        }
        #endregion
    }
}