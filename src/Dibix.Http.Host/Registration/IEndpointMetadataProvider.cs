using System.Collections.Generic;

namespace Dibix.Http.Host
{
    public interface IEndpointMetadataProvider
    {
        IEnumerable<EndpointDefinition> GetEndpoints();
    }
}