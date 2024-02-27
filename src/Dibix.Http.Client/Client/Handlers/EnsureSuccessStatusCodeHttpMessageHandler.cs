using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// Ensure success status code by throwing without disposing the response message content
    /// </summary>
    public sealed class EnsureSuccessStatusCodeHttpMessageHandler : DelegatingHandler
    {
        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // We are not using the builtin EnsureSuccessStatusCode method on HttpResponseMessage,
            // since it disposes the response content, before we can capture it for diagnostics.
            //responseMessage.EnsureSuccessStatusCode();
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Found)
                {
                    HttpClientHandler httpClientHandler = FindClientHandler(this);

                    // If automatic redirects are disabled, we don't want to throw for 302, because it might be expected
                    if (httpClientHandler is { AllowAutoRedirect: false })
                        return response;
                }
                throw await HttpException.Create(request, response).ConfigureAwait(false);
            }

            return response;
        }
        #endregion

        #region Private Methods
        private static HttpClientHandler FindClientHandler(HttpMessageHandler handler) => handler switch
        {
            HttpClientHandler clientHandler => clientHandler,
            DelegatingHandler delegatingHandler => FindClientHandler(delegatingHandler.InnerHandler),
            _ => null
        };
        #endregion
    }
}