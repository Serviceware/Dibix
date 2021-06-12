using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpClientBuilder
    {
        IHttpClientBuilder ConfigureClient(Action<HttpClient> configure);
        IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new();
        IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(THandler handler) where THandler : HttpMessageHandler;
        IHttpClientBuilder AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new();
        IHttpClientBuilder AddHttpMessageHandler(DelegatingHandler handler);
        IHttpClientBuilder RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler;
        IHttpClientBuilder RemoveHttpMessageHandler(DelegatingHandler handler);
        IHttpClientBuilder ClearHttpMessageHandlers();
    }
}