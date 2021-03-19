﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition
    {
        public ActionDefinitionTarget Target { get; }
        public ActionMethod Method { get; set; }
        public string Description { get; set; }
        public string ChildRoute { get; set; }
        public ActionRequestBody RequestBody { get; set; }
        public bool IsAnonymous { get; set; }
        public ActionFileResponse FileResponse { get; set; }
        public IList<ActionParameter> Parameters { get; }
        public IDictionary<HttpStatusCode, ActionResponse> Responses { get; }
        public ICollection<SecurityScheme> SecuritySchemes { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.Parameters = new Collection<ActionParameter>();
            this.Responses = new Dictionary<HttpStatusCode, ActionResponse>();
            this.SecuritySchemes = new Collection<SecurityScheme>();
        }

        public void SetDefaultResultType(TypeReference resultType)
        {
            HttpStatusCode statusCode = resultType != null ? HttpStatusCode.OK : HttpStatusCode.NoContent;
            if (!this.Responses.TryGetValue(statusCode, out ActionResponse response))
            {
                response = new ActionResponse(statusCode, resultType);
                this.Responses.Add(statusCode, response);
            }
            else
                response.ResultType = resultType;
        }
    }
}