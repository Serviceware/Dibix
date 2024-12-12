using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host
{
    internal sealed class HttpEndpointMetadataProvider(EndpointMetadataContext endpointMetadataContext) : IHttpEndpointMetadataProvider
    {
        public HttpActionDefinition GetActionDefinition() => endpointMetadataContext.Value.ActionDefinition;
    }
}