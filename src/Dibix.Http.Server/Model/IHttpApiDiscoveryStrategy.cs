using System.Collections.Generic;

namespace Dibix.Http.Server
{
    public interface IHttpApiDiscoveryStrategy
    {
        IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context);
    }
}