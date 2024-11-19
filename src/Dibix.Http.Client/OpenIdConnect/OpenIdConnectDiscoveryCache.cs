using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace Dibix.Http.Client.OpenIdConnect
{
    // Not using IdentityModel's builtin DiscoveryCache here, because it does not support adjusting the DiscoveryDocumentRequest.
    public sealed class OpenIdConnectDiscoveryCache : IOpenIdConnectDiscoveryCache
    {
        private readonly IDictionary<string, CacheItem> _responseCache = new ConcurrentDictionary<string, CacheItem>();

        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24d);

        public async Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync(string authority, Func<HttpMessageInvoker> httpClientFactory, Action<HttpRequestMessage> requestMessageFormatter = null)
        {
            if (!_responseCache.TryGetValue(authority, out CacheItem cacheItem) || cacheItem.NextReload <= DateTime.UtcNow)
            {
                DiscoveryDocumentResponse response = await GetDiscoveryDocument(authority, httpClientFactory, requestMessageFormatter).ConfigureAwait(false);
                DateTime nextReload = DateTime.UtcNow.Add(CacheDuration);
                cacheItem = new CacheItem(response, nextReload);
                _responseCache[authority] = cacheItem;
            }
            return cacheItem.Response;
        }

        private static async Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, Func<HttpMessageInvoker> httpClientFactory, Action<HttpRequestMessage> requestMessageFormatter)
        {
            using HttpMessageInvoker client = httpClientFactory();
            DiscoveryDocumentRequest request = new DiscoveryDocumentRequest { Address = authority };
            requestMessageFormatter?.Invoke(request);

            DiscoveryDocumentResponse response = await client.GetDiscoveryDocumentAsync(request).ConfigureAwait(false);
            if (response.Exception != null)
                throw response.Exception;

            if (response.IsError)
            {
                throw new InvalidOperationException(@$"Error while visiting OpenIdConnect discovery endpoint: {response.Error}
ErrorType: {response.ErrorType}
HttpErrorReason: {response.HttpErrorReason}");
            }

            return response;
        }

        private readonly struct CacheItem
        {
            public DiscoveryDocumentResponse Response { get; }
            public DateTime NextReload { get; }

            public CacheItem(DiscoveryDocumentResponse response, DateTime nextReload)
            {
                Response = response;
                NextReload = nextReload;
            }
        }
    }
}