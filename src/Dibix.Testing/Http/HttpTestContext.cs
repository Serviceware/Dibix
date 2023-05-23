using System;
using System.Net.Http;
using Dibix.Http.Client;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace Dibix.Testing.Http
{
    public class HttpTestContext<TService> : HttpTestContext where TService : IHttpService
    {
        public TService Service { get; }

        public HttpTestContext(TService service, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider) : base(httpClientFactory, httpAuthorizationProvider)
        {
            Service = service;
        }
    }

    public class HttpTestContext
    {
        internal IHttpClientFactory HttpClientFactory { get; }
        public IHttpAuthorizationProvider HttpAuthorizationProvider { get; }

        public HttpTestContext(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider)
        {
            HttpClientFactory = httpClientFactory;
            HttpAuthorizationProvider = httpAuthorizationProvider;
        }

        public TService CreateService<TService>() => HttpServiceFactory.CreateServiceInstance<TService>(HttpClientFactory, HttpAuthorizationProvider);

        public HttpClient CreateClient() => HttpClientFactory.CreateClient(TestHttpClientFactoryBuilder.HttpClientName);
    }
}