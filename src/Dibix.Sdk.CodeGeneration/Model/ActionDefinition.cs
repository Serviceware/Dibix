using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition : ActionTargetDefinition
    {
        public SourceLocation Location { get; set; }
        public ActionMethod Method { get; set; }
        public string OperationId { get; set; }
        public string Description { get; set; }
        public Token<string> ChildRoute { get; set; }
        public ActionRequestBody RequestBody { get; set; }
        public SecuritySchemeRequirements SecuritySchemes { get; } = new SecuritySchemeRequirements(SecuritySchemeOperator.Or);
        public ICollection<AuthorizationBehavior> Authorization { get; set; } = new Collection<AuthorizationBehavior>();
        public ActionCompatibilityLevel CompatibilityLevel { get; set; } = ActionCompatibilityLevel.Native;
        public ICollection<int> DisabledAutoDetectionStatusCodes { get; } = new HashSet<int>();
        public ActionFileResponse FileResponse { get; private set; }
        public TypeReference DefaultResponseType
        {
            get => GetDefaultResponseType();
            set => SetDefaultResponseType(value);
        }

        public ActionDefinition(ActionTarget actionTarget) : base(actionTarget) { }

        public void SetFileResponse(ActionFileResponse actionFileResponse, SourceLocation location)
        {
            FileResponse = actionFileResponse;
            Responses[HttpStatusCode.OK] = new ActionResponse(HttpStatusCode.OK, actionFileResponse.MediaType, resultType: ActionDefinitionUtility.CreateStreamTypeReference(location));
            
            // A custom error response might have already been registered
            if (!Responses.ContainsKey(HttpStatusCode.NotFound))
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