using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Http.Server
{
    public sealed class HttpActionDefinition : IHttpActionExecutionDefinition
    {
        private HttpControllerDefinition _controller;
        private string _fullName;

        public EndpointMetadata Metadata { get; }
        public HttpControllerDefinition Controller
        {
            get => _controller ?? throw new InvalidOperationException("Controller not initialized");
            internal set => _controller = value;
        }
        public string FullName => _fullName ??= GenerateFullName();
        public string ActionName { get; set; }
        public string RelativeNamespace { get; set; }
        public Uri Uri { get; set; }
        public IHttpActionExecutionMethod Executor { get; set; }
        public IHttpParameterResolutionMethod ParameterResolver { get; set; }
        public HttpApiMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public HttpRequestBody Body { get; set; }
        public HttpFileResponseDefinition FileResponse { get; set; }
        public string Description { get; set; }
        public ICollection<HttpAuthorizationDefinition> Authorization { get; } = new List<HttpAuthorizationDefinition>();
        public ICollection<string> SecuritySchemes { get; } = new Collection<string>();
        public IList<string> RequiredClaims { get; } = new Collection<string>();
        public IDictionary<int, HttpErrorResponse> StatusCodeDetectionResponses { get; }
        public Delegate Delegate { get; set; }
        public string[] ValidAudiences { get; set; }

        internal HttpActionDefinition(EndpointMetadata metadata)
        {
            Metadata = metadata;
            StatusCodeDetectionResponses = new Dictionary<int, HttpErrorResponse>();
        }

        private string GenerateFullName()
        {
            ICollection<string> tokens =
            [
                Metadata.ProductName,
                Metadata.AreaName,
                RelativeNamespace,
                ActionName
            ];
            string fullName = String.Join(".", tokens.Where(x => !String.IsNullOrEmpty(x)));
            return fullName;
        }
    }
}