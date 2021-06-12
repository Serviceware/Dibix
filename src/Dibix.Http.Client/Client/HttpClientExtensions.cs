using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken = default) => client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = new ObjectContent<T>(value, new JsonMediaTypeFormatter()) }, cancellationToken);

        public static HttpClient AddUserAgentFromAssembly(this HttpClient client, Assembly assembly, Func<string, string> productNameFormatter = null) => AddUserAgent(client, UserAgentResolver.FromAssembly(assembly, productNameFormatter));
        public static HttpClient AddUserAgentFromEntryAssembly(this HttpClient client, Func<string, string> productNameFormatter = null) => AddUserAgent(client, UserAgentResolver.FromEntryAssembly(productNameFormatter));
        private static HttpClient AddUserAgent(this HttpClient client, ProductInfoHeaderValue item)
        {
            client.DefaultRequestHeaders.UserAgent.Add(item);
            return client;
        }
    }
}