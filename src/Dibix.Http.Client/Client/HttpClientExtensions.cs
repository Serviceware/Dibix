using System;
using System.Diagnostics;
using System.IO;
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

        public static HttpClient AddUserAgentFromEntryAssembly(this HttpClient client, Func<string, string> productNameFormatter = null) => AddUserAgentFromAssembly(client, ResolveEntryAssembly(), productNameFormatter);
        public static HttpClient AddUserAgentFromAssembly(this HttpClient client, Assembly assembly, Func<string, string> productNameFormatter = null) => AddUserAgent(client, ResolveUserAgentFromAssembly(assembly, productNameFormatter));
        private static HttpClient AddUserAgent(this HttpClient client, ProductInfoHeaderValue item)
        {
            client.DefaultRequestHeaders.UserAgent.Add(item);
            return client;
        }
        
        private static ProductInfoHeaderValue ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string productName = Path.GetFileNameWithoutExtension(assembly.Location);
            string productVersion = fileVersionInfo.ProductVersion;

            if (productNameFormatter != null)
                productName = productNameFormatter(productName);

            return new ProductInfoHeaderValue(productName, productVersion);
        }

        private static Assembly ResolveEntryAssembly()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                throw new InvalidOperationException("Could not determine entry assembly");

            return assembly;
        }
    }
}