using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public class BearerAuthorizationHttpMessageHandler : DelegatingHandler
    {
        private readonly string _token;

        public BearerAuthorizationHttpMessageHandler() { }
        public BearerAuthorizationHttpMessageHandler(string token) => this._token = token;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string token = await this.GetToken(request).ConfigureAwait(false);
            if (String.IsNullOrEmpty(token))
                throw new InvalidOperationException("Bearer token is empty");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        protected virtual Task<string> GetToken(HttpRequestMessage requestMessage) => Task.FromResult(this._token);
    }
}