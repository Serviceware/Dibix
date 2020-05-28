using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Fields
        private readonly IControllerActionTargetSelector _controllerActionTargetSelector;
        private readonly ITypeResolverFacade _typeResolver;
        #endregion

        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider(IFileSystemProvider fileSystemProvider, IControllerActionTargetSelector controllerActionTargetSelector, ITypeResolverFacade typeResolver, ILogger logger, IEnumerable<string> endpoints) : base(fileSystemProvider, logger)
        {
            this._controllerActionTargetSelector = controllerActionTargetSelector;
            this._typeResolver = typeResolver;
            this.Controllers = new Collection<ControllerDefinition>();
            base.Collect(endpoints);
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json)
        {
            this.ReadControllers(filePath, json);
        }
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
            ActionDefinitionTarget actionTarget = this.ReadActionTarget(action, filePath);
            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);

            ActionDefinition actionDefinition = new ActionDefinition(actionTarget)
            {
                Method = method,
                Description = (string)action.Property("description")?.Value,
                ChildRoute = (string)action.Property("childRoute")?.Value,
                BodyContract = this.ReadBodyContract(action, filePath),
                BodyBinder = (string)action.Property("bindFromBody")?.Value,
                IsAnonymous = (bool?)action.Property("isAnonymous")?.Value ?? default
            };
            this.ReadControllerActionParameters(actionDefinition, (JObject)action.Property("params")?.Value, filePath);
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

        private ActionDefinitionTarget ReadActionTarget(JObject action, string filePath)
        {
            JToken targetValue = action.Property("target").Value;
            ActionDefinitionTarget actionTarget = this._controllerActionTargetSelector.Select((string)targetValue, filePath, targetValue);
            return actionTarget;
        }

        private TypeReference ReadBodyContract(JObject action, string filePath)
        {
            JToken bodyContractValue = action.Property("body")?.Value;
            if (bodyContractValue == null) 
                return null;

            string bodyContractTypeName = (string)bodyContractValue;
            IJsonLineInfo bodyContractLocation = bodyContractValue;
            bool isEnumerable = bodyContractTypeName.EndsWith("*", StringComparison.Ordinal);
            bodyContractTypeName = bodyContractTypeName.TrimEnd('*');
            TypeReference bodyContract = this._typeResolver.ResolveType(bodyContractTypeName, null, filePath, bodyContractLocation.LineNumber, bodyContractLocation.LinePosition, isEnumerable);
            return bodyContract;
        }

        private static void ReadControllerImport(ControllerDefinition controller, string typeName)
        {
            controller.ControllerImports.Add(typeName);
        }

        private void ReadControllerActionParameters(ActionDefinition action, JObject mappings, string filePath)
        {
            if (mappings == null)
                return;

            GeneratedAccessorMethodTarget generatedAccessorMethodTarget = action.Target as GeneratedAccessorMethodTarget;

            foreach (JProperty property in mappings.Properties())
            {
                if (generatedAccessorMethodTarget?.Parameters.ContainsKey(property.Name) == false)
                {
                    IJsonLineInfo propertyLocation = property;
                    base.Logger.LogError(null, $"Parameter '{property.Name}' not found on action: {action.Target.Name}", filePath, propertyLocation.LineNumber, propertyLocation.LinePosition);
                }

                if (TryReadSource(property, action.ParameterSources))
                    continue;

                switch (property.Value.Type)
                {
                    case JTokenType.Object:
                        ReadComplexActionParameter(action, property.Name, (JObject)property.Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(property.Value.Type), property.Value.Type, null);
                }
            }
        }

        private static bool TryReadSource(JProperty property, IDictionary<string, ActionParameterSource> target)
        {
            switch (property.Value.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Integer:
                    ReadConstantSource(property.Name, (JValue)property.Value, target);
                    return true;

                case JTokenType.String:
                    ReadPropertySource(property.Name, (JValue)property.Value, target);
                    return true;

                default:
                    return false;
            }
        }

        private static void ReadConstantSource(string parameterName, JValue value, IDictionary<string, ActionParameterSource> target)
        {
            target.Add(parameterName, new ActionParameterConstantSource(value.Value));
        }

        private static ActionParameterPropertySource ReadPropertySource(string parameterName, JValue value, IDictionary<string, ActionParameterSource> target)
        {
            string[] parts = ((string)value.Value).Split(new[] { '.' }, 2);
            ActionParameterPropertySource propertySource = new ActionParameterPropertySource(parts[0], parts[1]);
            target.Add(parameterName, propertySource);
            return propertySource;
        }

        private static void ReadComplexActionParameter(ActionDefinition action, string parameterName, JObject container)
        {
            JProperty bodyConverterProperty = container.Property("convertFromBody");
            if (bodyConverterProperty != null)
            {
                ReadBodyActionParameter(action, parameterName, bodyConverterProperty);
                return;
            }

            JProperty sourceProperty = container.Property("source");
            if (sourceProperty != null)
            {
                ReadPropertyActionParameter(action, parameterName, container, sourceProperty);
                return;
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static void ReadPropertyActionParameter(ActionDefinition action, string parameterName, JObject container, JProperty sourceProperty)
        {
            ActionParameterPropertySource propertySource = ReadPropertySource(parameterName, (JValue)sourceProperty.Value, action.ParameterSources);

            JProperty itemsProperty = container.Property("items");
            if (itemsProperty != null)
            {
                JObject itemsObject = (JObject)itemsProperty.Value;
                foreach (JProperty itemProperty in itemsObject.Properties())
                {
                    TryReadSource(itemProperty, propertySource.ItemSources);
                }
                return;
            }

            JProperty converterProperty = container.Property("converter");
            if (converterProperty != null)
            {
                propertySource.Converter = (string)((JValue)converterProperty.Value).Value;
                return;
            }

            throw new InvalidOperationException($"Invalid object for parameter: {parameterName}");
        }

        private static void ReadBodyActionParameter(ActionDefinition action, string parameterName, JProperty bodyConverterProperty)
        {
            string bodyConverterTypeName = (string)((JValue)bodyConverterProperty.Value).Value;
            action.ParameterSources.Add(parameterName, new ActionParameterBodySource(bodyConverterTypeName));
        }
        #endregion
    }
}
