﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        #region Fields
        private readonly HttpRequestTracer _httpRequestTracer;
        #endregion

        #region Properties
        public bool FollowRedirectsForGetRequests { get; set; } = true;
        #endregion

        #region Constructor
        public DefaultHttpClientFactory() { }
        public DefaultHttpClientFactory(HttpRequestTracer httpRequestTracer)
        {
            this._httpRequestTracer = httpRequestTracer;
        }
        #endregion

        #region IHttpClientFactory Members
        public HttpClient CreateClient() => this.CreateClient(baseAddress: null);
        public HttpClient CreateClient(Uri baseAddress)
        {
            HttpClientBuilder builder = new HttpClientBuilder();
            this.CreateClientCore(builder);
            this.CreateClient(builder);

            HttpMessageHandler handler = CreateHandlerPipeline(builder);
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = baseAddress;
            builder.ConfigureClient(client);

            return client;
        }
        #endregion

        #region Private Methods
        private static HttpMessageHandler CreateHandlerPipeline(HttpClientBuilder clientBuilder)
        {
            HttpMessageHandler next = clientBuilder.PrimaryHttpMessageHandler;
            for (int i = clientBuilder.AddtionalHandlers.Count - 1; i >= 0; i--)
            {
                DelegatingHandler handler = clientBuilder.AddtionalHandlers[i];

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
        private void CreateClientCore(IHttpClientBuilder builder)
        {
            builder.AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>();
            
            if (this.FollowRedirectsForGetRequests)
                builder.AddHttpMessageHandler<FollowRedirectHttpMessageHandler>();
            
            builder.AddHttpMessageHandler<TraceProxyHttpMessageHandler>();

            if (this._httpRequestTracer != null)
                builder.AddHttpMessageHandler(new TracingHttpMessageHandler(this._httpRequestTracer));
        }
        #endregion

        #region Nested Types
        private sealed class HttpClientBuilder : IHttpClientBuilder
        {
            private readonly ICollection<Action<HttpClient>> _clientActions;

            public HttpMessageHandler PrimaryHttpMessageHandler { get; set; } = new HttpClientHandler();
            public IList<DelegatingHandler> AddtionalHandlers { get; }

            public HttpClientBuilder()
            {
                this._clientActions = new Collection<Action<HttpClient>>();
                this.AddtionalHandlers = new Collection<DelegatingHandler>();
            }

            public void ConfigureClient(Action<HttpClient> configure)
            {
                Guard.IsNotNull(configure, nameof(configure));
                this._clientActions.Add(configure);
            }

            public void ConfigureClient(HttpClient client)
            {
                foreach (Action<HttpClient> configure in this._clientActions) 
                    configure(client);
            }

            public void ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new()
            {
                this.ConfigurePrimaryHttpMessageHandler(new THandler());
            }
            public void ConfigurePrimaryHttpMessageHandler<THandler>(THandler handler) where THandler : HttpMessageHandler
            {
                this.PrimaryHttpMessageHandler = handler;
            }

            public void AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new() => this.AddHttpMessageHandler(new THandler());
            public void AddHttpMessageHandler(DelegatingHandler handler)
            {
                Guard.IsNotNull(handler, nameof(handler));
                this.AddtionalHandlers.Add(handler);
            }

            public void RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new()
            {
                foreach (THandler handler in this.AddtionalHandlers.OfType<THandler>().ToArray()) 
                    this.AddtionalHandlers.Remove(handler);
            }
            public void RemoveHttpMessageHandler(DelegatingHandler handler) => this.AddtionalHandlers.Remove(handler);

            public void ClearHttpMessageHandlers() => this.AddtionalHandlers.Clear();
        }
        #endregion
    }

    public interface IHttpClientBuilder
    {
        void ConfigureClient(Action<HttpClient> configure);
        void ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new();
        void ConfigurePrimaryHttpMessageHandler<THandler>(THandler handler) where THandler : HttpMessageHandler;
        void AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new();
        void AddHttpMessageHandler(DelegatingHandler handler);
        void RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new();
        void RemoveHttpMessageHandler(DelegatingHandler handler);
        void ClearHttpMessageHandlers();
    }
}