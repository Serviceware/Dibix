﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionTargetDefinitionResolver
    {
        #region Properties
        protected ISchemaRegistry SchemaRegistry { get; }
        protected ILogger Logger { get; }
        #endregion

        #region Constructor
        protected ActionTargetDefinitionResolver(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this.SchemaRegistry = schemaRegistry;
            this.Logger = logger;
        }
        #endregion

        #region Abstract Methods
        public abstract bool TryResolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> readOnlyDictionary, ICollection<string> bodyParameters, ActionRequestBody requestBody, Func<ActionTarget, T> actionTargetDefinitionFactory, out T actionTargetDefinition) where T : ActionTargetDefinition;
        #endregion

        #region Protected Methods
        protected T CreateActionTargetDefinition<T>(ActionTarget actionTarget, IReadOnlyDictionary<string, PathParameter> pathParameters, ActionRequestBody requestBody, Func<ActionTarget, T> actionTargetDefinitionFactory) where T : ActionTargetDefinition
        {
            T actionTargetDefinition = actionTargetDefinitionFactory(actionTarget);
            actionTargetDefinition.PathParameters.AddRange(pathParameters);
            if (requestBody?.Contract != null)
                actionTargetDefinition.Parameters.Add(new ActionParameter("body", "body", requestBody.Contract, ActionParameterLocation.Body, isRequired: true, isOutput: false, defaultValue: null, sourceLocation: requestBody.Contract.Location, source: null));

            return actionTargetDefinition;
        }

        protected void CollectActionParameter
        (
            string parameterName
          , TypeReference parameterType
          , ValueReference defaultValue
          , bool isOutParameter
          , string actionName
          , SourceLocation sourceLocation
          , ActionParameterRegistry parameterRegistry
          , IReadOnlyDictionary<string, ExplicitParameter> explicitParameters
          , IReadOnlyDictionary<string, PathParameter> pathParameters
          , ICollection<string> bodyParameters
        )
        {
            ActionParameter actionParameter = this.CreateActionParameter(parameterName, parameterType, isOutParameter, defaultValue, explicitParameters, pathParameters, bodyParameters, sourceLocation);
            if (actionParameter != null)
                parameterRegistry.Add(actionParameter);
        }

        protected bool IsParameterRequired(TypeReference type, ActionParameterLocation location, ValueReference defaultValue)
        {
            switch (location)
            {
                case ActionParameterLocation.Query:
                    return defaultValue == null && Equals(type?.IsUserDefinedType(SchemaRegistry), false);

                case ActionParameterLocation.Header:
                    return defaultValue == null;

                default:
                    return true;
            }
        }

        protected bool ValidateImplicitParameterMetadata(ExplicitParameter explicitParameter)
        {
            if (explicitParameter.Type == null && explicitParameter.ParameterLocation == null && explicitParameter.DefaultValueBuilder == null)
                return true;

            Logger.LogError($"Metadata of parameter '{explicitParameter.Name}' is automatically detected for this action target and therefore should not be specified explicitly", explicitParameter.SourceLocation);
            return false;
        }

        protected static bool IsUserParameter(ActionParameterSourceDefinition source, string propertyName, ref ActionParameterLocation location, ref string apiParameterName)
        {
            switch (source)
            {
                case QueryParameterSource _:
                    location = ActionParameterLocation.Query;
                    return true;

                case PathParameterSource _:
                    location = ActionParameterLocation.Path;
                    return true;

                case BodyParameterSource _:
                    location = ActionParameterLocation.Body;
                    return true;

                case HeaderParameterSource _:
                    location = ActionParameterLocation.Header;
                    return true;

                case RequestParameterSource _ when propertyName == "Language":
                    location = ActionParameterLocation.Header;
                    apiParameterName = "Accept-Language";
                    return true;

                default:
                    return false;
            }
        }
        #endregion

        #region Private Methods
        private ActionParameter CreateActionParameter(string name, TypeReference type, bool isOutput, ValueReference defaultValue, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, SourceLocation sourceLocation)
        {
            ActionParameterLocation location = ActionParameterLocation.NonUser;
            string apiParameterName = name;
            string internalParameterName = name;

            ActionParameterSource source = null;
            if (explicitParameters.TryGetValue(name, out ExplicitParameter explicitParameter))
            {
                explicitParameter.Visited = true;

                if (!ValidateImplicitParameterMetadata(explicitParameter))
                    return null;

                source = explicitParameter.SourceBuilder.Build(type);

                PathParameter pathParameter;
                if (source is ActionParameterPropertySource propertySource)
                {
                    apiParameterName = propertySource.PropertyPath.Split('.')[0];
                    _ = IsUserParameter(propertySource.Definition, propertySource.PropertyPath, ref location, ref apiParameterName);

                    if (propertySource.Definition is PathParameterSource)
                    {
                        // Use case sensitive comparison, because the runtime does not support case insensitive argument resolution
                        if (!pathParameters.TryGetValue(apiParameterName, out pathParameter) || pathParameter.Name != apiParameterName)
                        {
                            Logger.LogError($"Property '{apiParameterName}' not found in path", propertySource.Location.Source, propertySource.Location.Line, propertySource.Location.Column);
                        }

                        if (pathParameter != null)
                        {
                            pathParameter.Visited = true;
                        }
                    }
                }

                // See CodeGenerationTaskTests.Endpoints
                // Here the user parameter is expected, but overwritten internally
                // Not sure if this is really a use case
                //
                // "childRoute": "{password}/Fixed",
                // "params": {
                //    "password": null
                // }
                if (pathParameters.TryGetValue(name, out pathParameter))
                {
                    location = ActionParameterLocation.Path;
                    pathParameter.Visited = true;
                }
            }
            else if (pathParameters.TryGetValue(name, out PathParameter pathParameter))
            {
                apiParameterName = pathParameter.Name;
                location = ActionParameterLocation.Path;
                pathParameter.Visited = true;
            }
            else if (bodyParameters.Contains(name))
            {
                location = ActionParameterLocation.Body;
            }
            else
            {
                if (defaultValue == null)
                {
                    // Inferring the parameter from query as a fallback was initially adapted from ASP.NET.
                    // However, it is very error-prone, because sometimes parameters are added to the SP,
                    // that are intended to be resolved from a parameter source, but are forgotten to be explicitly mapped.
                    // Then they end up on the client, which they shouldn't be.
                    // From now on they have to be explicitly mapped using the 'QUERY' parameter source.
                    //location = ActionParameterLocation.Query;
                    Logger.LogError($"Location of parameter '{name}' cannot be inferred. Please declare the source of the parameter.", sourceLocation);
                    return null;
                }
                location = ActionParameterLocation.Query;
            }

            bool isRequired = this.IsParameterRequired(type, location, defaultValue);
            return new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, isOutput, defaultValue, source, sourceLocation);
        }
        #endregion
    }
}