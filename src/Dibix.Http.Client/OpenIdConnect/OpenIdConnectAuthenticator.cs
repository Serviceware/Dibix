using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class OpenIdConnectAuthenticator : IOpenIdConnectAuthenticator
    {
        private readonly IOpenIdConnectDiscovery _openIdConnectDiscovery;
        private readonly IHttpClientFactory _httpClientFactory;

        public OpenIdConnectAuthenticator(IOpenIdConnectDiscovery openIdConnectDiscovery, IHttpClientFactory httpClientFactory)
        {
            this._openIdConnectDiscovery = openIdConnectDiscovery;
            this._httpClientFactory = httpClientFactory;
        }

        public Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password)
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password")
              , new KeyValuePair<string, string>("client_id", clientId)
              , new KeyValuePair<string, string>("username", userName)
              , new KeyValuePair<string, string>("password", password)
            });
            return this.CallTokenApi(authority, content);
        }

        public Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken)
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token")
              , new KeyValuePair<string, string>("client_id", clientId)
              , new KeyValuePair<string, string>("refresh_token", refreshToken)
            });
            return this.CallTokenApi(authority, content);
        }

        private async Task<TokenResponse> CallTokenApi(Uri authority, HttpContent content)
        {
            OpenIdConnectDiscoveryDocument discoveryDocument = await this._openIdConnectDiscovery.GetDiscoveryDocument(this._httpClientFactory, authority).ConfigureAwait(false);
            if (String.IsNullOrEmpty(discoveryDocument.TokenEndpoint))
                throw new InvalidOperationException("Could not find 'token_endpoint' in OpenIdConnect discovery document");

            using (HttpClient client = this._httpClientFactory.CreateClient())
            {
                HttpResponseMessage response = await client.PostAsync(discoveryDocument.TokenEndpoint, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                TokenResponse result = await response.Content.ReadAsAsync<TokenResponse>().ConfigureAwait(false);
                if (String.IsNullOrEmpty(result?.AccessToken))
                    throw new InvalidOperationException("Did not receive a valid credential from token endpoint");

                return result;
            }
        }
    }
}