using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Http.Server
{
    public sealed class HttpActionDefinition : IHttpActionExecutionDefinition
    {
        private HttpControllerDefinition _controller;

        public EndpointMetadata Metadata { get; }
        public HttpControllerDefinition Controller
        {
            get => _controller ?? throw new InvalidOperationException("Controller not initialized");
            internal set => _controller = value;
        }
        public string FullName { get; }
        public string ActionName { get; }
        public string RelativeNamespace { get; }
        public Uri Uri { get; }
        public IHttpActionExecutionMethod Executor { get; }
        public IHttpParameterResolutionMethod ParameterResolver { get; }
        public HttpApiMethod Method { get; }
        public string ChildRoute { get; }
        public HttpRequestBody Body { get; }
        public HttpFileResponseDefinition FileResponse { get; }
        public string Description { get; }
        public ModelContextProtocolType ModelContextProtocolType { get; }
        public Delegate Delegate { get; }
        public IReadOnlyCollection<HttpAuthorizationDefinition> Authorization { get; }
        public IReadOnlyCollection<string> SecuritySchemes { get; }
        public IReadOnlyList<string> RequiredClaims { get; }
        public IReadOnlyDictionary<int, HttpErrorResponse> StatusCodeDetectionResponses { get; }
        public ICollection<string> ValidAudiences { get; } = new Collection<string>();
        public IReadOnlyDictionary<string, string> ParameterDescriptions { get; }

        internal HttpActionDefinition
        (
            EndpointMetadata metadata,
            string actionName,
            string relativeNamespace,
            Uri uri,
            IHttpActionExecutionMethod executor,
            IHttpParameterResolutionMethod parameterResolver,
            HttpApiMethod method,
            string childRoute,
            HttpRequestBody body,
            HttpFileResponseDefinition fileResponse,
            string description,
            ModelContextProtocolType modelContextProtocolType,
            Delegate @delegate,
            IEnumerable<HttpAuthorizationDefinition> authorization,
            IEnumerable<string> securitySchemes,
            IEnumerable<string> requiredClaims,
            IReadOnlyDictionary<int, HttpErrorResponse> statusCodeDetectionResponses,
            IReadOnlyDictionary<string, string> parameterDescriptions
        )
        {
            Metadata = metadata;
            ActionName = actionName;
            RelativeNamespace = relativeNamespace;
            Uri = uri;
            Executor = executor;
            ParameterResolver = parameterResolver;
            Method = method;
            ChildRoute = childRoute;
            Body = body;
            FileResponse = fileResponse;
            Description = description;
            ModelContextProtocolType = modelContextProtocolType;
            Delegate = @delegate;
            Authorization = authorization.ToArray();
            SecuritySchemes = securitySchemes.ToArray();
            RequiredClaims = requiredClaims.ToArray();
            StatusCodeDetectionResponses = statusCodeDetectionResponses;
            ParameterDescriptions = parameterDescriptions;
            FullName = GenerateFullName();
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