using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;

namespace Dibix.Http.Client
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        #region Fields
        private readonly HttpClientFactoryConfiguration _configuration;
        #endregion

        #region Constructor
        public DefaultHttpClientFactory(Action<IHttpClientFactoryConfigurationBuilder> configure = null)
        {
            HttpClientFactoryConfiguration configuration = new HttpClientFactoryConfiguration();
            configure?.Invoke(configuration);
            this._configuration = configuration;
        }
        #endregion

        #region IHttpClientFactory Members
        public HttpClient CreateClient() => this.CreateClient(this._configuration.BaseAddress);
        public HttpClient CreateClient(Uri baseAddress)
        {
            HttpClientBuilder builder = new HttpClientBuilder();
            ICollection<Action<HttpClient>> postActions = new Collection<Action<HttpClient>>();
            this.CreateClientCore(builder, postActions);
            this.CreateClient(builder);

            HttpMessageHandler handler = CreateHandlerPipeline(builder);
            HttpClient client = new HttpClient(handler);
            
            client.BaseAddress = baseAddress;

            foreach (ProductInfoHeaderValue userAgent in this._configuration.UserAgent) 
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);

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
            for (int i = 0; i < clientBuilder.AddtionalHandlers.Count; i++)
            {
                DelegatingHandler handler = clientBuilder.AddtionalHandlers[i];
                if (!(handler is TracingHttpMessageHandler))
                    continue;

                clientBuilder.AddtionalHandlers.RemoveAt(i);
                clientBuilder.AddtionalHandlers.Add(handler);
                break;
            }

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
        private void CreateClientCore(IHttpClientBuilder builder, ICollection<Action<HttpClient>> postActions)
        {
            builder.AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>();
            
            if (this._configuration.FollowRedirectsForGetRequests)
                builder.AddHttpMessageHandler<FollowRedirectHttpMessageHandler>();
            
            builder.AddHttpMessageHandler<TraceProxyHttpMessageHandler>();

            if (this._configuration.HttpRequestTracer != null)
                builder.AddHttpMessageHandler(new TracingHttpMessageHandler(this._configuration.HttpRequestTracer));

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
        private sealed class HttpClientFactoryConfiguration : IHttpClientFactoryConfigurationBuilder
        {
            public Uri BaseAddress { get; set; }
            public HttpRequestTracer HttpRequestTracer { get; set; }
            public bool FollowRedirectsForGetRequests { get; set; } = true;
            public ICollection<ProductInfoHeaderValue> UserAgent { get; } = new Collection<ProductInfoHeaderValue>();

            public void AddUserAgent(Action<IHttpUserAgentSelectorExpression> selector)
            {
                HttpUserAgentSelectorExpression expression = new HttpUserAgentSelectorExpression();
                selector(expression);
                this.UserAgent.Add(expression.UserAgent);
            }
        }

        private sealed class HttpUserAgentSelectorExpression : IHttpUserAgentSelectorExpression
        {
            public ProductInfoHeaderValue UserAgent { get; private set; }

            public void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(assembly, productNameFormatter);

            public void FromEntryAssembly(Func<string, string> productNameFormatter = null) => this.ResolveUserAgentFromAssembly(ResolveEntryAssembly(), productNameFormatter);

            private void ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter)
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                string productName = fileVersionInfo.ProductName;
                string assemblyName = Path.GetFileNameWithoutExtension(assembly.Location);
                string productVersion = fileVersionInfo.ProductVersion;

                string userAgentProductName = $"{productName}{assemblyName}";
                if (productNameFormatter != null)
                    userAgentProductName = productNameFormatter(userAgentProductName);

                this.UserAgent = new ProductInfoHeaderValue(userAgentProductName, productVersion);
            }

            private static Assembly ResolveEntryAssembly()
            {
                Assembly assembly = Assembly.GetEntryAssembly();

                if (assembly == null)
                    throw new InvalidOperationException("Could not determine entry assembly");

                return assembly;
            }
        }

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
                this.AddtionalHandlers.Add(handler);
                return this;
            }

            public IHttpClientBuilder RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler
            {
                foreach (THandler handler in this.AddtionalHandlers.OfType<THandler>().ToArray()) 
                    this.AddtionalHandlers.Remove(handler);

                return this;
            }
            public IHttpClientBuilder RemoveHttpMessageHandler(DelegatingHandler handler)
            {
                this.AddtionalHandlers.Remove(handler);
                return this;
            }

            public IHttpClientBuilder ClearHttpMessageHandlers()
            {
                this.AddtionalHandlers.Clear();
                return this;
            }
        }
        #endregion
    }
}