using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTargetDefinition
    {
        public ActionTarget Target { get; }
        public IList<ActionParameter> Parameters { get; } = new Collection<ActionParameter>();
        public IDictionary<string, PathParameter> PathParameters { get; } = new Dictionary<string, PathParameter>();
        public IDictionary<HttpStatusCode, ActionResponse> Responses { get; } = new Dictionary<HttpStatusCode, ActionResponse>();

        protected ActionTargetDefinition(ActionTarget target)
        {
            Target = target;
        }

        public virtual void RegisterErrorResponse(int statusCode, int errorCode, string errorDescription, SourceLocation sourceLocation, ILogger logger)
        {
            HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
            if (!Responses.TryGetValue(httpStatusCode, out ActionResponse response))
            {
                response = new ActionResponse(httpStatusCode);
                Responses.Add(httpStatusCode, response);
            }
            response.AddError(errorCode, errorDescription, sourceLocation, logger);
        }
    }
}