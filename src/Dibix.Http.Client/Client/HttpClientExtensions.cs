using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public static bool TryGetSingleValue(this HttpHeaders headers, string name, out string value)
        {
            if (headers.TryGetValues(name, out IEnumerable<string> values))
            {
                value = values.First();
                return true;
            }

            value = null;
            return false;
        }

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
            
            public void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null)
            {
                UserAgent = UserAgentFactory.FromAssembly(assembly, productNameFormatter);
            }

            public void FromAssemblyContainingType<T>(Func<string, string> productNameFormatter = null)
            {
                UserAgent = UserAgentFactory.FromAssemblyContainingType<T>(productNameFormatter);
            }

            public void FromAssemblyContainingType(Type type, Func<string, string> productNameFormatter = null)
            {
                UserAgent = UserAgentFactory.FromAssemblyContainingType(type, productNameFormatter);
            }

            public void FromEntryAssembly(Func<string, string> productNameFormatter = null)
            {
                UserAgent = UserAgentFactory.FromEntryAssembly(productNameFormatter);
            }

            public void FromCurrentProcess(Func<string, string> productNameFormatter = null)
            {
                UserAgent = UserAgentFactory.FromCurrentProcess(productNameFormatter);
            }

            public void FromFile(string path, Func<string, string> productNameFormatter)
            {
                UserAgent = UserAgentFactory.FromFile(path, productNameFormatter);
            }

            public void FromCachedValue(ProductInfoHeaderValue value) => UserAgent = value;
        }
    }
}