using System;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host.Extensions
{
    internal static class HttpContextExtensions
    {
        public static EndpointDefinition GetEndpointDefinition(this HttpContext httpContext)
        {
            Endpoint? endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
                throw new InvalidOperationException("Could not retrieve endpoint from http context");

            EndpointDefinition? endpointDefinition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
            if (endpointDefinition == null)
                throw new InvalidOperationException("Could not retrieve endpoint definition from endpoint metadata");

            return endpointDefinition;
        }
    }
}