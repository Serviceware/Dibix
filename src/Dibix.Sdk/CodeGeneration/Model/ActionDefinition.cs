using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition
    {
        public ActionDefinitionTarget Target { get; }
        public ActionMethod Method { get; set; }
        public string OperationId { get; set; }
        public string Description { get; set; }
        public string ChildRoute { get; set; }
        public ActionRequestBody RequestBody { get; set; }
        public ActionFileResponse FileResponse { get; private set; }
        public IList<ActionParameter> Parameters { get; }
        public TypeReference DefaultResponseType
        {
            get => this.GetDefaultResponseType();
            set => this.SetDefaultResponseType(value);
        }
        public IDictionary<HttpStatusCode, ActionResponse> Responses { get; }
        public ICollection<ICollection<string>> SecuritySchemes { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.Parameters = new Collection<ActionParameter>();
            this.Responses = new Dictionary<HttpStatusCode, ActionResponse>();
            this.SecuritySchemes = new Collection<ICollection<string>>();
        }

        public void SetFileResponse(ActionFileResponse actionFileResponse, string source, int line, int column)
        {
            this.FileResponse = actionFileResponse;
            this.Responses[HttpStatusCode.OK] = new ActionResponse(HttpStatusCode.OK, actionFileResponse.MediaType, resultType: ActionDefinitionUtility.CreateStreamTypeReference(source, line, column));
            this.Responses[HttpStatusCode.NotFound] = new ActionResponse(HttpStatusCode.NotFound);
        }

        private TypeReference GetDefaultResponseType() => this.Responses.TryGetValue(HttpStatusCode.OK, out ActionResponse response) ? response.ResultType : null;

        private void SetDefaultResponseType(TypeReference typeReference)
        {
            HttpStatusCode statusCode = typeReference != null ? HttpStatusCode.OK : HttpStatusCode.NoContent;
            if (!this.Responses.TryGetValue(statusCode, out ActionResponse response))
            {
                response = new ActionResponse(statusCode, typeReference);
                this.Responses.Add(statusCode, response);
            }
            else
                response.ResultType = typeReference;
        }
    }
}