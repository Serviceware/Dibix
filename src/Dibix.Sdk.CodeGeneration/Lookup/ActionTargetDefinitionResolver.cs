using System.Collections.Generic;
using System.Net;
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
        public abstract bool TryResolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> readOnlyDictionary, ICollection<string> bodyParameters, out T actionTargetDefinition) where T : ActionTargetDefinition, new();
        #endregion

        #region Protected Methods
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
            if (isOutParameter)
            {
                //base.Logger.LogError(null, $"Output parameters are not supported in endpoints: {actionName}", filePath, line, column);

                // We don't support out parameters in REST APIs, so we assume that this method is used as both backend accessor and REST API.
                // Therefore we just skip it
                return;
            }

            ActionParameter actionParameter = this.CreateActionParameter(parameterName, parameterType, defaultValue, explicitParameters, pathParameters, bodyParameters, sourceLocation);
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

        protected static void RegisterErrorResponse(ActionTargetDefinition actionTargetDefinition, int statusCode, int errorCode, string errorDescription)
        {
            HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
            if (!actionTargetDefinition.Responses.TryGetValue(httpStatusCode, out ActionResponse response))
            {
                response = new ActionResponse(httpStatusCode);
                actionTargetDefinition.Responses.Add(httpStatusCode, response);
            }
            response.Errors.Add(new ErrorDescription(errorCode, errorDescription));
        }
        #endregion

        #region Private Methods
        private ActionParameter CreateActionParameter(string name, TypeReference type, ValueReference defaultValue, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, SourceLocation sourceLocation)
        {
            ActionParameterLocation location = ActionParameterLocation.NonUser;
            string apiParameterName = name;
            string internalParameterName = name;

            ActionParameterSource source = null;
            if (explicitParameters.TryGetValue(name, out ExplicitParameter explicitParameter))
            {
                explicitParameter.Visited = true;
                source = explicitParameter.SourceBuilder.Build(type);

                PathParameter pathParameter;
                if (source is ActionParameterPropertySource propertySource)
                {
                    apiParameterName = propertySource.PropertyName.Split('.')[0];
                    _ = IsUserParameter(propertySource.Definition, propertySource.PropertyName, ref location, ref apiParameterName);

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
                location = ActionParameterLocation.Query;
            }

            bool isRequired = this.IsParameterRequired(type, location, defaultValue);
            return new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, defaultValue, source, sourceLocation);
        }
        #endregion
    }
}