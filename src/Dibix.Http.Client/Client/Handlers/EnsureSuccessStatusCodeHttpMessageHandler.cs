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
                if (IsRedirectStatusCode(response))
                {
                    HttpClientHandler httpClientHandler = FindClientHandler(this);

                    // If automatic redirects are disabled, it is intended to return the status code to the application without throwing
                    if (httpClientHandler is { AllowAutoRedirect: false })
                        return response;
                }
                throw await HttpException.Create(request, response).ConfigureAwait(false);
            }

            return response;
        }
        #endregion

        #region Private Methods
        private static bool IsRedirectStatusCode(HttpResponseMessage response)
        {
            int statusCode = (int)response.StatusCode;
            return statusCode >= 300 && statusCode < 400;
        }

        private static HttpClientHandler FindClientHandler(HttpMessageHandler handler) => handler switch
        {
            HttpClientHandler clientHandler => clientHandler,
            DelegatingHandler delegatingHandler => FindClientHandler(delegatingHandler.InnerHandler),
            _ => null
        };
        #endregion
    }
}