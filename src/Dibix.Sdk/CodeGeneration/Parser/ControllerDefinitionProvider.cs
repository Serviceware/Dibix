using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dibix.Http;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _outputName;
        private readonly ICollection<SqlStatementInfo> _statements;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider(string productName, string areaName, string outputName, ICollection<SqlStatementInfo> statements, IEnumerable<string> endpoints, ITypeResolverFacade typeResolver, ReferencedAssemblyInspector referencedAssemblyInspector, ISchemaRegistry schemaRegistry, IFileSystemProvider fileSystemProvider, ILogger logger) : base(fileSystemProvider, logger)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._outputName = outputName;
            this._statements = statements;
            this._typeResolver = typeResolver;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._schemaRegistry = schemaRegistry;
            this.Controllers = new Collection<ControllerDefinition>();
            base.Collect(endpoints);
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
                        ReadControllerImport(controller, (string)((JValue)action).Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(action.Type), action.Type, null);
                }
            }
            this.Controllers.Add(controller);
        }

        private void ReadControllerAction(string filePath, ControllerDefinition controller, JObject action)
        {
            // Collect explicit parameters
            IDictionary<string, ExplicitParameter> explicitParameters = new Dictionary<string, ExplicitParameter>();
            CollectControllerActionParameters((JObject)action.Property("params")?.Value, explicitParameters);

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
            TypeReference bodyContract = this.ReadBodyContract(action, filePath);
            ICollection<string> bodyParameters = GetBodyProperties(bodyContract, this._schemaRegistry);

            // Resolve action target, parameters and create action definition
            ActionDefinition actionDefinition = this.CreateActionDefinition(action, filePath, explicitParameters, pathParameters, bodyParameters);

            // Unfortunately we do not have any metadata on reflection targets
            if (!(actionDefinition.Target is ReflectionActionTarget))
            {
                // Validate explicit parameters
                foreach (ExplicitParameter explicitParameter in explicitParameters.Values)
                {
                    IJsonLineInfo propertyLocation = explicitParameter.Property;
                    base.Logger.LogError(null, $"Parameter '{explicitParameter.Property.Name}' not found on action: {actionDefinition.Target.Name}", filePath, propertyLocation.LineNumber, explicitParameter.Property.GetCorrectLinePosition());
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
            actionDefinition.BodyContract = bodyContract;
            actionDefinition.BodyBinder = (string)action.Property("bindFromBody")?.Value;
            actionDefinition.IsAnonymous = (bool?)action.Property("isAnonymous")?.Value ?? default;

            if (controller.Actions.Add(actionDefinition))
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

        private TypeReference ReadBodyContract(JObject action, string filePath)
        {
            JValue bodyContractValue = (JValue)action.Property("body")?.Value;
            if (bodyContractValue == null) 
                return null;

            string bodyContractTypeName = (string)bodyContractValue;
            IJsonLineInfo bodyContractLocation = bodyContractValue;
            bool isEnumerable = bodyContractTypeName.EndsWith("*", StringComparison.Ordinal);
            bodyContractTypeName = bodyContractTypeName.TrimEnd('*');
            TypeReference bodyContract = this._typeResolver.ResolveType(bodyContractTypeName, null, filePath, bodyContractLocation.LineNumber, bodyContractValue.GetCorrectLinePosition(), isEnumerable);
            return bodyContract;
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

        private static void CollectControllerActionParameters(JObject mappings, IDictionary<string, ExplicitParameter> target)
        {
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
                            throw new ArgumentOutOfRangeException(nameof(property.Value.Type), property.Value.Type, null);
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
            bool isExternal = target.Contains(",");
            if (isExternal)
                return new ActionDefinition(new ReflectionActionTarget(target));

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
            ActionDefinition actionDefinition;

            // 2. Target is a SQL statement within the current project
            SqlStatementInfo statement = this._statements.FirstOrDefault(x => x.Namespace == normalizedNamespace && x.Name == methodName);
            if (statement != null)
            {
                ActionDefinitionTarget actionTarget = new LocalActionTarget(statement, this._outputName);
                actionDefinition = new ActionDefinition(actionTarget);
                ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
                foreach (SqlQueryParameter parameter in statement.Parameters)
                {
                    CollectActionParameter(parameter.Name, parameter.Type, parameter.HasDefaultValue, parameter.DefaultValue, parameterRegistry, explicitParameters, pathParameters, bodyParameters);
                }
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
            if (!this.TryGetNeighborActionTarget(methodName, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out actionDefinition))
            {
                base.Logger.LogError(null, $"Could not find a method named '{methodName}' on database accessor type '{typeName}'", filePath, line, column);
                return null;
            }
            return actionDefinition;
        }

        private bool TryGetNeighborActionTarget(string methodName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters, out ActionDefinition actionDefinition)
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
                            select this.CreateActionDefinition(method, filePath, line, column, explicitParameters, pathParameters, bodyParameters);

                return query.FirstOrDefault();
            });

            return actionDefinition != null;
        }

        private ActionDefinition CreateActionDefinition(MethodInfo method, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            string operationName = method.Name;
            Type returnType = method.ReturnType;
            bool isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);

            if (isAsync)
            {
                returnType = returnType.GenericTypeArguments[0];
                const string asyncSuffix = "Async";
                if (operationName.EndsWith(asyncSuffix, StringComparison.Ordinal))
                    operationName = operationName.Remove(operationName.Length - asyncSuffix.Length);
            }

            TypeReference resultType = null;
            if (returnType != typeof(void))
                resultType = ReflectionTypeResolver.ResolveType(returnType, filePath, line, column, this._schemaRegistry);

            NeighborActionTarget target = new NeighborActionTarget(method.DeclaringType.FullName, resultType, operationName, isAsync);
            ActionDefinition actionDefinition = new ActionDefinition(target);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
            method.CollectErrorResponses((statusCode, errorCode, errorDescription, isClientError) => target.ErrorResponses.Add(new ErrorResponse(statusCode, errorCode, errorDescription, isClientError)));

            IEnumerable<ParameterInfo> parameters = method.GetExternalParameters(isAsync);
            foreach (ParameterInfo parameter in parameters)
            {
                string parameterName = parameter.Name;
                TypeReference parameterType = ReflectionTypeResolver.ResolveType(parameter.ParameterType, filePath, line, column, this._schemaRegistry);

                // ParameterInfo.HasDefaultValue/DefaultValue => It is illegal to reflect on the custom attributes of a Type loaded via ReflectionOnlyGetType (see Assembly.ReflectionOnly) -- use CustomAttributeData instead
                bool hasDefaultValue = parameter.RawDefaultValue != DBNull.Value;
                object defaultValue = parameter.RawDefaultValue;

                CollectActionParameter(parameterName, parameterType, hasDefaultValue, defaultValue, parameterRegistry, explicitParameters, pathParameters, bodyParameters);
            }

            return actionDefinition;
        }

        private static void CollectActionParameter(string name, TypeReference type, bool hasDefaultValue, object defaultValue, ActionParameterRegistry parameterRegistry, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            ActionParameter actionParameter = CreateActionParameter(name, type, hasDefaultValue, defaultValue, explicitParameters, pathParameters, bodyParameters);
            parameterRegistry.Add(actionParameter);
        }

        private static ActionParameter CreateActionParameter(string name, TypeReference type, bool hasDefaultValue, object defaultValue, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, Group> pathParameters, ICollection<string> bodyParameters)
        {
            ActionParameterLocation location = ActionParameterLocation.NonUser;

            if (explicitParameters.TryGetValue(name, out ExplicitParameter explicitParameter))
            {
                explicitParameters.Remove(name);

                if (explicitParameter.Source is ActionParameterPropertySource propertySource
                 && TryGetLocation(propertySource.SourceName, ref location))
                {
                    name = propertySource.PropertyName;

                    if (location == ActionParameterLocation.Path)
                        pathParameters.Remove(propertySource.PropertyName);
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
                name = pathSegment.Value;
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

            return new ActionParameter(name, type, location, hasDefaultValue, defaultValue, explicitParameter?.Source);
        }

        private static bool TryGetLocation(string sourceName, ref ActionParameterLocation location)
        {
            switch (sourceName)
            {
                case "QUERY":
                    location = ActionParameterLocation.Query;
                    return true;

                case "PATH":
                    location = ActionParameterLocation.Path;
                    return true;

                default:
                    return false;
            }
        }
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
                    int currentPathSegmentIndex = this._pathSegmentIndexMap[actionParameter.Name];
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
