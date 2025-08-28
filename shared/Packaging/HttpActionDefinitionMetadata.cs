using System;
using System.Collections.Generic;
using Dibix.Http;

namespace Dibix
{
    internal sealed class HttpActionDefinitionMetadata
    {
        public string ActionName { get; }
        public string RelativeNamespace { get; }
        public Uri Uri { get; set; }
        public HttpApiMethod Method { get; }
        public string ChildRoute { get; }
        public HttpFileResponseDefinition FileResponse { get; }
        public string Description { get; }
        public ModelContextProtocolType ModelContextProtocolType { get; }
        public IReadOnlyCollection<string> SecuritySchemes { get; }
        public IReadOnlyCollection<string> RequiredClaims { get; }
        public IReadOnlyDictionary<int, HttpErrorResponse> StatusCodeDetectionResponses { get; }
        public IReadOnlyCollection<string> ValidAudiences { get; }

        public HttpActionDefinitionMetadata(string actionName, string relativeNamespace, Uri uri, HttpApiMethod method, string childRoute, HttpFileResponseDefinition fileResponse, string description, ModelContextProtocolType modelContextProtocolType, IReadOnlyCollection<string> securitySchemes, IReadOnlyCollection<string> requiredClaims, IReadOnlyDictionary<int, HttpErrorResponse> statusCodeDetectionResponses, IReadOnlyCollection<string> validAudiences)
        {
            ActionName = actionName;
            RelativeNamespace = relativeNamespace;
            Uri = uri;
            Method = method;
            ChildRoute = childRoute;
            FileResponse = fileResponse;
            Description = description;
            ModelContextProtocolType = modelContextProtocolType;
            SecuritySchemes = securitySchemes ?? [];
            RequiredClaims = new SortedSet<string>(requiredClaims ?? []);
            StatusCodeDetectionResponses = statusCodeDetectionResponses ?? new Dictionary<int, HttpErrorResponse>();
            ValidAudiences = validAudiences ?? [];
        }
    }
}