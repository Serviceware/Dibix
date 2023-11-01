using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Server
{
    public sealed class HttpActionDefinition : IHttpActionExecutionDefinition
    {
        public HttpControllerDefinition Controller { get; internal set; }
        public Uri Uri { get; set; }
        public IHttpActionExecutionMethod Executor { get; set; }
        public IHttpParameterResolutionMethod ParameterResolver { get; set; }
        public HttpApiMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public HttpRequestBody Body { get; set; }
        public HttpFileResponseDefinition FileResponse { get; set; }
        public string Description { get; set; }
        public HttpAuthorizationDefinition Authorization { get; set; }
        public ICollection<string> SecuritySchemes { get; } = new Collection<string>();
        public ICollection<string> RequiredClaims { get; } = new Collection<string>();
        public IDictionary<int, HttpErrorResponse> StatusCodeDetectionResponses { get; }
        public Delegate Delegate { get; set; }

        internal HttpActionDefinition()
        {
            StatusCodeDetectionResponses = new Dictionary<int, HttpErrorResponse>();
        }
    }
}