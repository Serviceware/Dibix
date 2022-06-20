using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectDiscovery
    {
        Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority);
        Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority, Action<HttpRequestMessage> requestFormatter);
        Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, string httpClientName, Uri authority);
        Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, string httpClientName, Uri authority, Action<HttpRequestMessage> requestFormatter);
        void InvalidateCache(Uri authority);
    }
}