using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTargetDefinition
    {
        public ActionTarget Target { get; set; }
        public IList<ActionParameter> Parameters { get; }
        public IDictionary<HttpStatusCode, ActionResponse> Responses { get; }
        public TypeReference DefaultResponseType
        {
            get => GetDefaultResponseType();
            set => SetDefaultResponseType(value);
        }
        public ActionFileResponse FileResponse { get; private set; }

        protected ActionTargetDefinition()
        {
            Parameters = new Collection<ActionParameter>();
            Responses = new Dictionary<HttpStatusCode, ActionResponse>();
        }

        public void SetFileResponse(ActionFileResponse actionFileResponse, string source, int line, int column)
        {
            FileResponse = actionFileResponse;
            Responses[HttpStatusCode.OK] = new ActionResponse(HttpStatusCode.OK, actionFileResponse.MediaType, resultType: ActionDefinitionUtility.CreateStreamTypeReference(source, line, column));
            Responses[HttpStatusCode.NotFound] = new ActionResponse(HttpStatusCode.NotFound);
        }

        private TypeReference GetDefaultResponseType() => Responses.TryGetValue(HttpStatusCode.OK, out ActionResponse response) ? response.ResultType : null;

        private void SetDefaultResponseType(TypeReference typeReference)
        {
            HttpStatusCode statusCode = typeReference != null ? HttpStatusCode.OK : HttpStatusCode.NoContent;
            if (!Responses.TryGetValue(statusCode, out ActionResponse response))
            {
                response = new ActionResponse(statusCode, typeReference);
                Responses.Add(statusCode, response);
            }
            else
                response.ResultType = typeReference;
        }
    }
}