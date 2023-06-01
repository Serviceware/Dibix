using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Testing.Http
{
    public sealed class OfflineHttpClientConfiguration
    {
        public string Name => "Dibix.Testing.Http.OfflineHttpClient";

        public void Configure(IHttpClientBuilder builder)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new OfflineHttpMessageHandler());
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