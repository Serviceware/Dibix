using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class OpenIdConnectDiscovery : IOpenIdConnectDiscovery
    {
        private readonly IDictionary<Uri, DiscoveryDocumentCacheItem> _cache = new ConcurrentDictionary<Uri, DiscoveryDocumentCacheItem>();

        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24d);

        public Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority) => GetDiscoveryDocument(httpClientFactory, DefaultHttpClientFactory.DefaultClientName, authority, requestFormatter: null);
        public Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority, Action<HttpRequestMessage> requestFormatter) => GetDiscoveryDocument(httpClientFactory, DefaultHttpClientFactory.DefaultClientName, authority, requestFormatter);
        public Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, string httpClientName, Uri authority) => GetDiscoveryDocument(httpClientFactory, httpClientName, authority, requestFormatter: null);
        public async Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, string httpClientName, Uri authority, Action<HttpRequestMessage> requestFormatter)
        {
            Uri sanitizedAuthority = EnsureTrailingSlash(authority);
            if (!this._cache.TryGetValue(sanitizedAuthority, out DiscoveryDocumentCacheItem cacheItem) || cacheItem.NextReload <= DateTime.UtcNow)
            {
                cacheItem = await this.CollectDiscoveryDocument(httpClientFactory, httpClientName, sanitizedAuthority, requestFormatter).ConfigureAwait(false);
                this._cache[sanitizedAuthority] = cacheItem;
            }
            return cacheItem.Document;
        }

        public void InvalidateCache(Uri authority) => this._cache.Remove(authority);

        private async Task<DiscoveryDocumentCacheItem> CollectDiscoveryDocument(IHttpClientFactory httpClientFactory, string httpClientName, Uri sanitizedAuthority, Action<HttpRequestMessage> requestFormatter)
        {
            using (HttpClient client = httpClientFactory.CreateClient(httpClientName))
            {
                client.BaseAddress = sanitizedAuthority;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ".well-known/openid-configuration");
                requestFormatter?.Invoke(request);

                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                OpenIdConnectDiscoveryDocument document = await response.Content.ReadAsAsync<OpenIdConnectDiscoveryDocument>().ConfigureAwait(false);
                DateTime nextReload = DateTime.UtcNow.Add(this.CacheDuration);
                return new DiscoveryDocumentCacheItem(document, nextReload);
            }
        }

        private static Uri EnsureTrailingSlash(Uri uri) => uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{uri}/");

        private readonly struct DiscoveryDocumentCacheItem
        {
            public OpenIdConnectDiscoveryDocument Document { get; }
            public DateTime NextReload { get; }

            public DiscoveryDocumentCacheItem(OpenIdConnectDiscoveryDocument document, DateTime nextReload)
            {
                this.Document = document;
                this.NextReload = nextReload;
            }
        }
    }
}