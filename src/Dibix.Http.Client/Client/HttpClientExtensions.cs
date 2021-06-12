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

        public static HttpClient AddUserAgent(this HttpClient client, Action<IHttpUserAgentSelectorExpression> selector)
        {
            HttpUserAgentSelectorExpression expression = new HttpUserAgentSelectorExpression();
            selector(expression);
            return AddUserAgent(client, expression.UserAgent);
        }
        private static HttpClient AddUserAgent(this HttpClient client, ProductInfoHeaderValue item)
        {
            client.DefaultRequestHeaders.UserAgent.Add(item);
            return client;
        }

        private sealed class HttpUserAgentSelectorExpression : IHttpUserAgentSelectorExpression
        {
            public ProductInfoHeaderValue UserAgent { get; private set; }

            public void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(assembly, productNameFormatter);

            public void FromEntryAssembly(Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(ResolveEntryAssembly(), productNameFormatter);

            private void ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter)
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                string productName = fileVersionInfo.ProductName;
                string assemblyName = Path.GetFileNameWithoutExtension(assembly.Location);
                string productVersion = fileVersionInfo.ProductVersion;

                string userAgentProductName = $"{productName}{assemblyName}";
                if (productNameFormatter != null)
                    userAgentProductName = productNameFormatter(userAgentProductName);

                this.UserAgent = new ProductInfoHeaderValue(userAgentProductName, productVersion);
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
}