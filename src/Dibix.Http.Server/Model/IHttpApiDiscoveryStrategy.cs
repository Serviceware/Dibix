using System.Collections.Generic;

namespace Dibix.Http.Server
{
    internal interface IHttpApiDiscoveryStrategy
    {
        IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context);
    }
}