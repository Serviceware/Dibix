using System;
using System.Net.Http;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public class HttpTestContext<TService> : HttpTestContext where TService : IHttpService
    {
        public TService Service { get; }

        public HttpTestContext(TService service, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider) : base(httpClientFactory, httpAuthorizationProvider)
        {
            this.Service = service;
        }
    }

    public class HttpTestContext
    {
        internal IHttpClientFactory HttpClientFactory { get; }
        public IHttpAuthorizationProvider HttpAuthorizationProvider { get; }

        public HttpTestContext(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider)
        {
            this.HttpClientFactory = httpClientFactory;
            this.HttpAuthorizationProvider = httpAuthorizationProvider;
        }

        public TService CreateService<TService>() => HttpServiceFactory.CreateServiceInstance<TService>(this.HttpClientFactory, this.HttpAuthorizationProvider);

        public HttpClient CreateClient() => this.HttpClientFactory.CreateClient(TestHttpClientConfiguration.HttpClientName);
        public HttpClient CreateClient(Uri baseAddress) => this.HttpClientFactory.CreateClient(TestHttpClientConfiguration.HttpClientName, baseAddress);
    }
}