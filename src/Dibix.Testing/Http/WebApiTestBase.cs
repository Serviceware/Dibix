using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TConfiguration> : DatabaseTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Fields
        private static readonly Type[] ConstructorSignature =
        {
            typeof(IHttpClientFactory),
            typeof(IHttpAuthorizationProvider)
        };
        #endregion

        #region Protected Methods
        private protected Task ExecuteTest(Func<IHttpTestContext<TConfiguration>, Task> testFlow) => this.ExecuteTest(testFlow, CreateTestContext);

        protected virtual async Task ExecuteTest<TTestContext>(Func<TTestContext, Task> testFlow, Func<TConfiguration, IHttpClientFactory, IHttpAuthorizationProvider, TTestContext> contextCreator) where TTestContext : IHttpTestContext<TConfiguration>
        {
            TConfiguration configuration = base.LoadConfiguration();

            IHttpClientFactory httpClientFactory = new HttpClientFactory(base.Out, x => this.ConfigureClient(configuration, x));
            IHttpAuthorizationProvider authorizationProvider = await this.Authorize(httpClientFactory, configuration).ConfigureAwait(false);
            TTestContext testContext = contextCreator(configuration, httpClientFactory, authorizationProvider);
            await testFlow(testContext).ConfigureAwait(false);
        }

        protected Task ExecuteTestUnit<TService>(TService service, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => ExecuteTestUnit<TService, HttpResponseMessage>(service, methodSelector);
        protected async Task<TContent> ExecuteTestUnit<TService, TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText)
        {
            HttpResponse<TContent> response = await ExecuteTestUnit(service, methodSelector).ConfigureAwait(false);
            this.Assert(response, expectedText);
            return response.ResponseContent;
        }
        private protected Task<TContent> ExecuteTestUnit<TService, TContent>(IHttpTestContext<TConfiguration> context, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText)
        {
            TService service = CreateServiceInstance<TService>(context.HttpClientFactory, context.HttpAuthorizationProvider);
            return this.ExecuteTestUnit(service, methodSelector, expectedText);
        }

        private protected static TService CreateServiceInstance<TService>(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider)
        {
            Type contractType = typeof(TService);
            Type implementationType = ResolveImplementationType(contractType);
            ConstructorInfo constructor = ResolveConstructor(implementationType);
            TService service = (TService)constructor.Invoke(new object[] { httpClientFactory, authorizationProvider });
            return service;
        }

        protected virtual void ConfigureClient(TConfiguration configuration, IHttpClientBuilder builder) { }

        protected virtual Task<IHttpAuthorizationProvider> Authorize(IHttpClientFactory httpClientFactory, TConfiguration configuration) => Task.FromResult<IHttpAuthorizationProvider>(null);
        #endregion

        #region Private Methods
        private static async Task<TResponse> ExecuteTestUnit<TService, TResponse>(TService service, Expression<Func<TService, Task<TResponse>>> methodSelector)
        {
            Func<TService, Task<TResponse>> compiled = methodSelector.Compile();
            Task<TResponse> task = compiled(service);
            TResponse response = await task.ConfigureAwait(false);
            return response;
        }

        private void Assert<TContent>(HttpResponse<TContent> response, string expectedText)
        {
            string actualText = JsonConvert.SerializeObject(response.ResponseContent, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                Formatting = Formatting.Indented
            });
            JToken actualTextDom = JToken.Parse(actualText);
            string expectedTextReplaced = Regex.Replace(expectedText, @"\{(?<path>[A-Za-z.]+)\}", x =>
            {
                string path = x.Groups["path"].Value;
                if (!(actualTextDom.SelectToken(path) is JValue value) || value.Value == null)
                    throw new InvalidOperationException($"Replace pattern did not match a JSON path in the actual document: {path} ({x.Index})");

                return value.Value.ToString();
            });
            base.AssertEqualDiffTool(expectedTextReplaced, actualText);
        }

        private static Type ResolveImplementationType(Type contractType)
        {
            foreach (Type type in contractType.Assembly.GetTypes())
            {
                HttpServiceAttribute attribute = type.GetCustomAttribute<HttpServiceAttribute>();
                if (attribute?.ContractType == contractType)
                    return type;
            }

            throw new InvalidOperationException($"Could not determine server implementation for type '{contractType}'. Is it a HTTP service generated by Dibix?");
        }

        private static ConstructorInfo ResolveConstructor(Type implementationType)
        {
            foreach (ConstructorInfo constructor in implementationType.GetConstructors())
            {
                if (ConstructorSignature.SequenceEqual(constructor.GetParameters().Select(x => x.ParameterType)))
                    return constructor;
            }

            throw new InvalidOperationException($"Could not find constructor ({String.Join(", ", ConstructorSignature.Select(x => x.ToString()))}) on type: {implementationType}");
        }

        private static IHttpTestContext<TConfiguration> CreateTestContext(TConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider)
        {
            return new HttpTestContext(configuration, httpClientFactory, authorizationProvider);
        }
        #endregion

        #region Nested Types
        protected class HttpTestContext : IHttpTestContext<TConfiguration>
        {
            public TConfiguration Configuration { get; }
            public IHttpClientFactory HttpClientFactory { get; }
            public IHttpAuthorizationProvider HttpAuthorizationProvider { get; }

            public HttpTestContext(TConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider)
            {
                this.Configuration = configuration;
                this.HttpClientFactory = httpClientFactory;
                this.HttpAuthorizationProvider = httpAuthorizationProvider;
            }
        }

        private sealed class HttpClientFactory : DefaultHttpClientFactory, IHttpClientFactory
        {
            private readonly TextWriter _logger;
            private readonly Action<IHttpClientBuilder> _additionalClientConfiguration;

            public HttpClientFactory(TextWriter logger, Action<IHttpClientBuilder> additionalClientConfiguration)
            {
                this._logger = logger;
                this._additionalClientConfiguration = additionalClientConfiguration;
            }

            protected override void CreateClient(IHttpClientBuilder builder)
            {
                this._additionalClientConfiguration(builder);
                builder.AddHttpMessageHandler(new LoggingHttpMessageHandler(this._logger));
            }
        }

        private sealed class LoggingHttpMessageHandler : DelegatingHandler
        {
            private readonly TextWriter _output;

            public LoggingHttpMessageHandler(TextWriter output) => this._output = output;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await this._output.WriteAsync($"{request.Method} {request.RequestUri}").ConfigureAwait(false);
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    sw.Stop();
                    await this._output.WriteAsync($" {(int)response.StatusCode} {response.StatusCode} {sw.Elapsed}").ConfigureAwait(false);
                    return response;
                }
                finally
                {
                    await this._output.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }
        #endregion
    }
}