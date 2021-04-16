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
                throw await HttpException.Create(request, response).ConfigureAwait(false);

            return response;
        }
        #endregion
    }
}