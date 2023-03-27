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

            public void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(assembly, productNameFormatter);
            
            public void FromEntryAssembly(Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(ResolveEntryAssembly(), productNameFormatter);
            
            public void FromCurrentProcess(Func<string, string> productNameFormatter = null) => this.FromFile(ResolveCurrentProcessPath(), productNameFormatter);

            public void FromFile(string path, Func<string, string> productNameFormatter)
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                string productName = fileVersionInfo.ProductName;
                string assemblyName = Path.GetFileNameWithoutExtension(path);
                string productVersion = fileVersionInfo.ProductVersion;

                string userAgentProductName = $"{productName}{assemblyName}";
                if (productNameFormatter != null)
                    userAgentProductName = productNameFormatter(userAgentProductName);

                this.UserAgent = new ProductInfoHeaderValue(userAgentProductName, productVersion);
            }

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
            private void ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter) => this.FromFile(assembly.Location, productNameFormatter);
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file

            private static Assembly ResolveEntryAssembly()
            {
                Assembly assembly = Assembly.GetEntryAssembly();

                if (assembly == null)
                    throw new InvalidOperationException("Could not determine entry assembly");

                return assembly;
            }

            private static string ResolveCurrentProcessPath()
            {
                ProcessModule processModule = Process.GetCurrentProcess().MainModule;
                if (processModule == null)
                    throw new InvalidOperationException("Could not determine main module from current process");
                
                string filePath = processModule.FileName;
                return filePath;
            }
        }
    }
}