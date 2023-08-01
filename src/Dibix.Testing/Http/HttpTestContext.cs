using System;
using System.Net.Http;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public sealed class HttpTestContext<TService> : HttpTestContext where TService : IHttpService
    {
        public TService Service { get; }

        internal HttpTestContext(TService service, IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider httpAuthorizationProvider) : base(httpClientFactory, httpClientOptions, httpAuthorizationProvider)
        {
            Service = service;
        }
    }

    public class HttpTestContext
    {
        internal IHttpClientFactory HttpClientFactory { get; }
        internal HttpClientOptions HttpClientOptions { get; }
        public IHttpAuthorizationProvider HttpAuthorizationProvider { get; }

        internal HttpTestContext(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider httpAuthorizationProvider)
        {
            HttpClientFactory = httpClientFactory;
            HttpClientOptions = httpClientOptions;
            HttpAuthorizationProvider = httpAuthorizationProvider;
        }

        public TService CreateService<TService>() => HttpServiceFactory.CreateServiceInstance<TService>(HttpClientFactory, HttpClientOptions, HttpAuthorizationProvider);
        public TService CreateService<TService>(IHttpAuthorizationProvider authorizationProvider) => HttpServiceFactory.CreateServiceInstance<TService>(HttpClientFactory, HttpClientOptions, authorizationProvider);

        public HttpClient CreateClient() => HttpClientFactory.CreateClient(TestHttpClientFactoryBuilder.HttpClientName);

        public void ConfigureClient(Action<HttpClientOptions> configure) => configure(HttpClientOptions);
    }
}