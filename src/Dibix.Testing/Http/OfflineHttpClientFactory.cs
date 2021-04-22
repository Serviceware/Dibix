using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public sealed class OfflineHttpClientFactory : DefaultHttpClientFactory, IHttpClientFactory
    {
        protected override void CreateClient(IHttpClientBuilder builder)
        {
            builder.ConfigurePrimaryHttpMessageHandler(new OfflineHttpMessageHandler());
        }

        private sealed class OfflineHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(responseMessage);
            }
        }
    }
}