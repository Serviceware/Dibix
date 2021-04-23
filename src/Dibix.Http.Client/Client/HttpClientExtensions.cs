using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken = default) => client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = new ObjectContent<T>(value, new JsonMediaTypeFormatter()) }, cancellationToken);
    }
}