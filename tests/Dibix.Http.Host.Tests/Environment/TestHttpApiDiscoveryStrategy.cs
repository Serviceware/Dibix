using System.Collections.Generic;
using Dibix.Http.Server;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TestHttpApiDiscoveryStrategy : IHttpApiDiscoveryStrategy
    {
        public IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context)
        {
            HttpApiDescriptor apiDescriptor = new TestHttpApiDescriptor();
            apiDescriptor.Configure(context);
            yield return apiDescriptor;
        }
    }
}