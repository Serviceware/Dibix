using System;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectDiscovery
    {
        Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority);
        void InvalidateCache(Uri authority);
    }
}