using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace Dibix.Http.Client
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        #region Fields
        private readonly Uri _baseAddress;
        private readonly HttpRequestTracer _httpRequestTracer;
        #endregion

        #region Properties
        public bool FollowRedirectsForGetRequests { get; set; } = true;
        #endregion

        #region Constructor
        public DefaultHttpClientFactory() { }
        public DefaultHttpClientFactory(Uri baseAddress) => this._baseAddress = baseAddress;
        public DefaultHttpClientFactory(HttpRequestTracer httpRequestTracer) => this._httpRequestTracer = httpRequestTracer;
        public DefaultHttpClientFactory(Uri baseAddress, HttpRequestTracer httpRequestTracer)
        {
            this._baseAddress = baseAddress;
            this._httpRequestTracer = httpRequestTracer;
        }
        #endregion

        #region IHttpClientFactory Members
        public HttpClient CreateClient() => this.CreateClient(this._baseAddress);
        public HttpClient CreateClient(Uri baseAddress)
        {
            HttpClientBuilder builder = new HttpClientBuilder();
            ICollection<Action<HttpClient>> postActions = new Collection<Action<HttpClient>>();
            this.CreateClientCore(builder, postActions);
            this.CreateClient(builder);

            HttpMessageHandler handler = CreateHandlerPipeline(builder);
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = baseAddress;
            builder.ConfigureClient(client);

            foreach (Action<HttpClient> postAction in postActions)
                postAction(client);

            return client;
        }
        #endregion

        #region Private Methods
        private static HttpMessageHandler CreateHandlerPipeline(HttpClientBuilder clientBuilder)
        {
            // Subsequent handlers, that make their own requests with the same tracer, might overwrite the last request trace
            // Therefore we move the tracing handler before user handlers
            for (int i = 0; i < clientBuilder.AdditionalHandlers.Count; i++)
            {
                DelegatingHandler handler = clientBuilder.AdditionalHandlers[i];
                if (!(handler is TracingHttpMessageHandler))
                    continue;

                clientBuilder.AdditionalHandlers.RemoveAt(i);
                clientBuilder.AdditionalHandlers.Add(handler);
                break;
            }

            HttpMessageHandler next = clientBuilder.PrimaryHttpMessageHandler;
            for (int i = clientBuilder.AdditionalHandlers.Count - 1; i >= 0; i--)
            {
                DelegatingHandler handler = clientBuilder.AdditionalHandlers[i];

                // Checking for this allows us to catch cases where someone has tried to re-use a handler. That really won't
                // work the way you want and it can be tricky for callers to figure out.
                if (handler.InnerHandler != null)
                {
                    string message = $@"The '{nameof(DelegatingHandler.InnerHandler)}' property must be null. '{nameof(DelegatingHandler)}' instances provided to '{nameof(IHttpClientBuilder)}' must not be reused or cached.
Handler: '{handler}'";
                    throw new InvalidOperationException(message);
                }

                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }
        #endregion

        #region Protected Methods
        protected virtual void CreateClient(IHttpClientBuilder builder) { }
        #endregion

        #region Private Methods
        private void CreateClientCore(IHttpClientBuilder builder, ICollection<Action<HttpClient>> postActions)
        {
            builder.AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>();
            
            if (this.FollowRedirectsForGetRequests)
                builder.AddHttpMessageHandler<FollowRedirectHttpMessageHandler>();
            
            builder.AddHttpMessageHandler<TraceProxyHttpMessageHandler>();

            if (this._httpRequestTracer != null)
                builder.AddHttpMessageHandler(new TracingHttpMessageHandler(this._httpRequestTracer));

            TimeoutHttpMessageHandler timeoutHandler = new TimeoutHttpMessageHandler();
            builder.AddHttpMessageHandler(timeoutHandler);
            postActions.Add(x =>
            {
                timeoutHandler.Timeout = x.Timeout;
                x.Timeout = Timeout.InfiniteTimeSpan;
            });
        }
        #endregion

        #region Nested Types
        private sealed class HttpClientBuilder : IHttpClientBuilder
        {
            private readonly ICollection<Action<HttpClient>> _clientActions;

            public HttpMessageHandler PrimaryHttpMessageHandler { get; set; } = new HttpClientHandler();
            public IList<DelegatingHandler> AdditionalHandlers { get; }

            public HttpClientBuilder()
            {
                this._clientActions = new Collection<Action<HttpClient>>();
                this.AdditionalHandlers = new Collection<DelegatingHandler>();
            }

            public IHttpClientBuilder ConfigureClient(Action<HttpClient> configure)
            {
                Guard.IsNotNull(configure, nameof(configure));
                this._clientActions.Add(configure);
                return this;
            }

            public IHttpClientBuilder ConfigureClient(HttpClient client)
            {
                foreach (Action<HttpClient> configure in this._clientActions) 
                    configure(client);

                return this;
            }

            public IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new()
            {
                this.ConfigurePrimaryHttpMessageHandler(new THandler());
                return this;
            }
            public IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(THandler handler) where THandler : HttpMessageHandler
            {
                this.PrimaryHttpMessageHandler = handler;
                return this;
            }

            public IHttpClientBuilder AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new() => this.AddHttpMessageHandler(new THandler());
            public IHttpClientBuilder AddHttpMessageHandler(DelegatingHandler handler)
            {
                Guard.IsNotNull(handler, nameof(handler));
                this.AdditionalHandlers.Add(handler);
                return this;
            }

            public IHttpClientBuilder RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler
            {
                foreach (THandler handler in this.AdditionalHandlers.OfType<THandler>().ToArray()) 
                    this.AdditionalHandlers.Remove(handler);

                return this;
            }
            public IHttpClientBuilder RemoveHttpMessageHandler(DelegatingHandler handler)
            {
                this.AdditionalHandlers.Remove(handler);
                return this;
            }

            public IHttpClientBuilder ClearHttpMessageHandlers()
            {
                this.AdditionalHandlers.Clear();
                return this;
            }
        }
        #endregion
    }
}