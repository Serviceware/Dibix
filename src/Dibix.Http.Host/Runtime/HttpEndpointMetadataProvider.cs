using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    internal sealed class HttpEndpointMetadataProvider(EndpointMetadataContext endpointMetadataContext) : IHttpEndpointMetadataProvider
    {
        public HttpActionDefinition GetActionDefinition() => endpointMetadataContext.Value.ActionDefinition;
    }
}