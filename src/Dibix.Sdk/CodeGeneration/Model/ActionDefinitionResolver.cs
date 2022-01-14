using System.Collections.Generic;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionDefinitionResolver
    {
        #region Properties
        protected ISchemaRegistry SchemaRegistry { get; }
        protected ILogger Logger { get; }
        #endregion

        #region Constructor
        protected ActionDefinitionResolver(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this.SchemaRegistry = schemaRegistry;
            this.Logger = logger;
        }
        #endregion

        #region Abstract Methods
        public abstract bool TryResolve(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out ActionDefinition actionDefinition);
        #endregion

        #region Protected Methods
        protected void CollectActionParameter
        (
            string parameterName
          , TypeReference parameterType
          , ValueReference defaultValue
          , bool isOutParameter
          , string actionName
          , string filePath
          , int line
          , int column
          , ActionParameterRegistry parameterRegistry
          , IDictionary<string, ExplicitParameter> explicitParameters
          , IDictionary<string, PathParameter> pathParameters
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

        protected bool IsParameterRequired(TypeReference type, ActionParameterLocation location, ValueReference defaultValue)
        {
            switch (location)
            {
                case ActionParameterLocation.Query:
                    return defaultValue == null && Equals(type?.IsUserDefinedType(this.SchemaRegistry), false);

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

        protected static void RegisterErrorResponse(ActionDefinition actionDefinition, int statusCode, int errorCode, string errorDescription)
        {
            HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
            if (!actionDefinition.Responses.TryGetValue(httpStatusCode, out ActionResponse response))
            {
                response = new ActionResponse(httpStatusCode);
                actionDefinition.Responses.Add(httpStatusCode, response);
            }
            response.Errors.Add(new ErrorDescription(errorCode, errorDescription));
        }
        #endregion

        #region Private Methods
        private ActionParameter CreateActionParameter(string name, TypeReference type, ValueReference defaultValue, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters)
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
                    if (IsUserParameter(propertySource.Definition, propertySource.PropertyName, ref location, ref apiParameterName))
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
            else if (pathParameters.TryGetValue(name, out PathParameter pathParameter))
            {
                apiParameterName = pathParameter.Name;
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

            bool isRequired = this.IsParameterRequired(type, location, defaultValue);
            return new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, defaultValue, explicitParameter?.Source);
        }
        #endregion
    }
}