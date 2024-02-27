using System.Net.Http;
using Dibix.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddBuiltinHttpMessageHandlers(this IHttpClientBuilder builder)
        {
            AddHttpMessageHandler<TraceProxyHttpMessageHandler>(builder);
            AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>(builder);
            AddHttpMessageHandler<TraceSourceHttpMessageHandler>(builder);
            return builder;
        }

        private static void AddHttpMessageHandler<T>(IHttpClientBuilder builder) where T : DelegatingHandler, new()
        {
            builder.AddHttpMessageHandler(() => new T());
        }
    }
}