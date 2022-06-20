using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpClientBuilder
    {
        bool EnsureSuccessStatusCode { get; set; }
        bool FollowRedirectsGetRequests { get; set; }
        bool WrapTimeoutsInException { get; set; }
        bool TraceProxy { get; set; }
        HttpRequestTracer Tracer { get; set; }

        IHttpClientBuilder ConfigureClient(Action<HttpClient> configure);
        IHttpClientBuilder AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new();
        IHttpClientBuilder AddHttpMessageHandler(Func<DelegatingHandler> handlerFactory);
        IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new();
        IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(Func<THandler> handlerFactory) where THandler : HttpMessageHandler;
    }
}