using System;
using System.Net.Http;
using Dibix.Http.Client;
using Microsoft.Extensions.DependencyInjection;
using IHttpClientBuilder = Microsoft.Extensions.DependencyInjection.IHttpClientBuilder;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace Dibix.Testing.Http
{
    internal abstract class HttpClientFactoryBuilder
    {
        private Action<IHttpClientBuilder> _configure;

        protected abstract string ClientName { get; }

        public HttpClientFactoryBuilder Configure(Action<IHttpClientBuilder> configure)
        {
            _configure = configure;
            return this;
        }

        public IHttpClientFactory Build()
        {
            IServiceCollection services = new ServiceCollection();

            IHttpClientBuilder httpClientBuilder = services.AddHttpClient(ClientName, Configure);
            ConfigureHttpClientDefaults(httpClientBuilder);
            Configure(httpClientBuilder);
            _configure?.Invoke(httpClientBuilder);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory;
        }

        protected virtual void Configure(IHttpClientBuilder builder) { }

        protected virtual void Configure(HttpClient client) { }

        private static void ConfigureHttpClientDefaults(IHttpClientBuilder httpClientBuilder)
        {
            AddHttpMessageHandler<FollowRedirectHttpMessageHandler>(httpClientBuilder);
            AddHttpMessageHandler<TraceProxyHttpMessageHandler>(httpClientBuilder);
            AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>(httpClientBuilder);
        }

        private static void AddHttpMessageHandler<T>(IHttpClientBuilder httpClientBuilder) where T : DelegatingHandler, new()
        {
            httpClientBuilder.AddHttpMessageHandler(() => new T());
        }
    }
}