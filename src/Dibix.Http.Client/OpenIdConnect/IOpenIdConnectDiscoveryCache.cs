using System;
using System.Net.Http;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectDiscoveryCache
    {
        TimeSpan CacheDuration { get; set; }

        Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync(string authority, Func<HttpMessageInvoker> httpClientFactory, Action<HttpRequestMessage> requestMessageFormatter = null);
    }
}