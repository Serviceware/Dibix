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
        private readonly string _httpClientName;

        public OpenIdConnectAuthenticator(IOpenIdConnectDiscovery openIdConnectDiscovery, IHttpClientFactory httpClientFactory) : this(openIdConnectDiscovery, httpClientFactory, DefaultHttpClientFactory.DefaultClientName) { }
        public OpenIdConnectAuthenticator(IOpenIdConnectDiscovery openIdConnectDiscovery, IHttpClientFactory httpClientFactory, string httpClientName)
        {
            this._openIdConnectDiscovery = openIdConnectDiscovery;
            this._httpClientFactory = httpClientFactory;
            this._httpClientName = httpClientName;
        }

        public Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password) => Login(authority, clientId, userName, password, requestFormatter: null);
        public Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password, Action<HttpRequestMessage> requestFormatter)
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password")
              , new KeyValuePair<string, string>("client_id", clientId)
              , new KeyValuePair<string, string>("username", userName)
              , new KeyValuePair<string, string>("password", password)
            });
            return this.CallTokenApi(authority, content, requestFormatter);
        }

        public Task<TokenResponse> Login(Uri authority, string clientId, string clientSecret) => Login(authority, clientId, clientSecret, requestFormatter: null);
        public Task<TokenResponse> Login(Uri authority, string clientId, string clientSecret, Action<HttpRequestMessage> requestFormatter)
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
              , new KeyValuePair<string, string>("client_id", clientId)
              , new KeyValuePair<string, string>("client_secret", clientSecret)
            });
            return this.CallTokenApi(authority, content, requestFormatter);
        }

        public Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken) => RefreshToken(authority, clientId, refreshToken, requestFormatter: null);
        public Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken, Action<HttpRequestMessage> requestFormatter)
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token")
              , new KeyValuePair<string, string>("client_id", clientId)
              , new KeyValuePair<string, string>("refresh_token", refreshToken)
            });
            return this.CallTokenApi(authority, content, requestFormatter);
        }

        private async Task<TokenResponse> CallTokenApi(Uri authority, HttpContent content, Action<HttpRequestMessage> requestFormatter)
        {
            OpenIdConnectDiscoveryDocument discoveryDocument = await this._openIdConnectDiscovery.GetDiscoveryDocument(this._httpClientFactory, this._httpClientName, authority, requestFormatter).ConfigureAwait(false);
            if (String.IsNullOrEmpty(discoveryDocument.TokenEndpoint))
                throw new InvalidOperationException("Could not find 'token_endpoint' in OpenIdConnect discovery document");

            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName))
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, discoveryDocument.TokenEndpoint) { Content = content };
                requestFormatter?.Invoke(request);

                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                TokenResponse result = await response.Content.ReadAsAsync<TokenResponse>().ConfigureAwait(false);
                if (String.IsNullOrEmpty(result?.AccessToken))
                    throw new InvalidOperationException("Did not receive a valid credential from token endpoint");

                return result;
            }
        }
    }
}