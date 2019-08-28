﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerDefinitionProvider : JsonSchemaDefinitionReader, IControllerDefinitionProvider
    {
        #region Properties
        public ICollection<ControllerDefinition> Controllers { get; }
        protected override string SchemaName => "dibix.endpoints.schema";
        #endregion

        #region Constructor
        public ControllerDefinitionProvider(IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter, IEnumerable<string> endpoints) : base(fileSystemProvider, errorReporter)
        {
            this.Controllers = new Collection<ControllerDefinition>();
            base.Collect(endpoints);
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json)
        {
            this.ReadControllers(json);
        }
        #endregion

        #region Private Methods
        private void ReadControllers(JObject apis)
        {
            foreach (JProperty apiProperty in apis.Properties())
            {
                if (apiProperty.Name == "$schema")
                    continue;

                this.ReadController(apiProperty.Name, (JArray)apiProperty.Value);
            }
        }

        private void ReadController(string controllerName, JArray actions)
        {
            ControllerDefinition controller = new ControllerDefinition(controllerName);
            foreach (JToken action in actions)
            {
                switch (action.Type)
                {
                    case JTokenType.Object:
                        ReadControllerAction(controller, (JObject)action);
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

        private static void ReadControllerAction(ControllerDefinition controller, JObject action)
        {
            string target = (string)action.Property("target").Value;
            ActionDefinitionTarget actionTarget = new ActionDefinitionTarget(target.Contains(","), target);
            Enum.TryParse((string)action.Property("method")?.Value, true, out ActionMethod method);
            ActionDefinition actionDefinition = new ActionDefinition(actionTarget)
            {
                Method = method,
                ChildRoute = (string)action.Property("childRoute")?.Value,
                BodyContract = (string)action.Property("body")?.Value,
                BodyBinder = (string)action.Property("bindFromBody")?.Value,
                OmitResult = (bool?)action.Property("omitResult")?.Value ?? default,
                IsAnonymous = (bool?)action.Property("isAnonymous")?.Value ?? default
            };
            ReadControllerActionParameters(actionDefinition, (JObject)action.Property("params")?.Value);
            controller.Actions.Add(actionDefinition);
        }

        private static void ReadControllerImport(ControllerDefinition controller, string typeName)
        {
            controller.ControllerImports.Add(typeName);
        }

        private static void ReadControllerActionParameters(ActionDefinition action, JObject mappings)
        {
            if (mappings == null)
                return;

            foreach (JProperty property in mappings.Properties())
            {
                switch (property.Value.Type)
                {
                    case JTokenType.Boolean:
                    case JTokenType.Integer:
                        ReadConstantActionParameter(action, property.Name, (JValue)property.Value);
                        break;

                    case JTokenType.String:
                        ReadPropertyActionParameter(action, property.Name, (JValue)property.Value);
                        break;

                    case JTokenType.Object:
                        ReadComplexActionParameter(action, property.Name, (JObject)property.Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(property.Value.Type), property.Value.Type, null);
                }
            }
        }

        private static void ReadConstantActionParameter(ActionDefinition action, string parameterName, JValue value)
        {
            action.DynamicParameters.Add(parameterName, new ActionParameterConstantSource(value.Value));
        }

        private static void ReadPropertyActionParameter(ActionDefinition action, string parameterName, JValue value)
        {
            string[] parts = ((string)value.Value).Split(new [] { '.' }, 2);
            action.DynamicParameters.Add(parameterName, new ActionParameterPropertySource(parts[0], parts[1]));
        }

        private static void ReadComplexActionParameter(ActionDefinition action, string parameterName, JObject @object)
        {
            string bodyConverterTypeName = (string)((JValue)@object.Property("convertFromBody").Value).Value;
            action.DynamicParameters.Add(parameterName, new ActionParameterBodySource(bodyConverterTypeName));
        }
        #endregion
    }
}
