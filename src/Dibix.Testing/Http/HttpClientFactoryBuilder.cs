﻿using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

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

            IHttpClientBuilder httpClientBuilder = services.AddHttpClient(ClientName, Configure)
                                                           .AddBuiltinHttpMessageHandlers();

            Configure(httpClientBuilder);
            _configure?.Invoke(httpClientBuilder);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory;
        }

        protected virtual void Configure(IHttpClientBuilder builder) { }

        protected virtual void Configure(HttpClient client) { }

        private static void AddHttpMessageHandler<T>(IHttpClientBuilder httpClientBuilder) where T : DelegatingHandler, new()
        {
            httpClientBuilder.AddHttpMessageHandler(() => new T());
        }
    }
}