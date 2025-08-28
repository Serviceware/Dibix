using System.Collections.Generic;

namespace Dibix.Http.Host
{
    internal sealed class EndpointJwtAudienceProvider : IJwtAudienceProvider
    {
        private readonly EndpointMetadataContext _endpointMetadataContext;

        public EndpointJwtAudienceProvider(EndpointMetadataContext endpointMetadataContext) => _endpointMetadataContext = endpointMetadataContext;

        public ICollection<string> GetValidAudiences() => _endpointMetadataContext.ValidAudiences;
    }
}