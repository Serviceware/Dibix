using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Dibix.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddBuiltinHttpMessageHandlers(this IHttpClientBuilder builder, params Type[] excludedHandlers)
        {
            AddHttpMessageHandler<TraceProxyHttpMessageHandler>(builder, excludedHandlers);
            AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>(builder, excludedHandlers);
            AddHttpMessageHandler<TraceSourceHttpMessageHandler>(builder, excludedHandlers);
            return builder;
        }

        private static void AddHttpMessageHandler<T>(IHttpClientBuilder builder, IEnumerable<Type> excludedHandlers) where T : DelegatingHandler, new()
        {
            if (excludedHandlers.Contains(typeof(T)))
                return;

            builder.AddHttpMessageHandler(() => new T());
        }
    }
}