using System.Collections.Generic;

namespace Dibix.Http.Host
{
    internal sealed class EndpointJwtAudienceProvider : IJwtAudienceProvider
    {
        private readonly EndpointMetadataContext _endpointMetadataContext;

        public EndpointJwtAudienceProvider(EndpointMetadataContext endpointMetadataContext) => _endpointMetadataContext = endpointMetadataContext;

        public IEnumerable<string>? GetValidAudiences() => _endpointMetadataContext.Value.ActionDefinition.ValidAudiences;
    }
}