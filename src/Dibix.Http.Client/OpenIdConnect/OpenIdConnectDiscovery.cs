using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class OpenIdConnectDiscovery : IOpenIdConnectDiscovery
    {
        private readonly IDictionary<Uri, OpenIdConnectDiscoveryDocument> _cache;

        public OpenIdConnectDiscovery()
        {
            this._cache = new Dictionary<Uri, OpenIdConnectDiscoveryDocument>();
        }

        public async Task<OpenIdConnectDiscoveryDocument> GetDiscoveryDocument(IHttpClientFactory httpClientFactory, Uri authority)
        {
            Uri sanitizedAuthority = EnsureTrailingSlash(authority);
            if (!this._cache.TryGetValue(sanitizedAuthority, out OpenIdConnectDiscoveryDocument document))
            {
                using (HttpClient client = httpClientFactory.Create())
                {
                    client.BaseAddress = sanitizedAuthority;
                    HttpResponseMessage response = await client.GetAsync(".well-known/openid-configuration").ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    document = await response.Content.ReadAsAsync<OpenIdConnectDiscoveryDocument>().ConfigureAwait(false);
                    this._cache.Add(sanitizedAuthority, document);
                }
            }
            return document;
        }

        public void InvalidateCache(Uri authority) => this._cache.Remove(authority);

        private static Uri EnsureTrailingSlash(Uri uri) => uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{uri}/");
    }
}