using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using IHttpClientBuilder = Microsoft.Extensions.DependencyInjection.IHttpClientBuilder;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TConfiguration> : DatabaseTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<HttpTestContext, Task> testFlow) => ExecuteTest(testFlow, CreateTestContext);

        private protected async Task ExecuteTest<TTestContext>(Func<TTestContext, Task> testFlow, Func<IHttpClientFactory, HttpClientOptions, IHttpAuthorizationProvider, TTestContext> contextCreator) where TTestContext : HttpTestContext
        {
            IHttpClientFactory httpClientFactory = TestHttpClientFactoryBuilder.Create(TestContext, Out)
                                                                               .Configure(x => ConfigureClient(Configuration, x))
                                                                               .Build();
            HttpClientOptions httpClientOptions = new HttpClientOptions
            {
                ResponseContent =
                {
                    // Do not convert to local time, to ensure timezone agnostic response assertions in tests
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                }
            };
            ITestAuthorizationContext authorizationContext = new TestAuthorizationContext(httpClientFactory, httpClientOptions);
            IHttpAuthorizationProvider authorizationProvider = await Authorize(authorizationContext, Configuration).ConfigureAwait(false);
            TTestContext testContext = contextCreator(httpClientFactory, httpClientOptions, authorizationProvider);
            await testFlow(testContext).ConfigureAwait(false);
        }

        protected Task InvokeApi<TService>(TService service, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => InvokeApi<TService, HttpResponseMessage>(service, methodSelector);
        protected async Task<TContent> InvokeApi<TService, TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText)
        {
            HttpResponse<TContent> response = await InvokeApi(service, methodSelector).ConfigureAwait(false);
            Assert(response, expectedText);
            return response.ResponseContent;
        }
        protected Task InvokeApi<TService>(HttpTestContext context, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => InvokeApiCore<TService, HttpResponseMessage>(context, methodSelector);
        protected async Task<TContent> InvokeApi<TService, TContent>(HttpTestContext context, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector)
        {
            HttpResponse<TContent> response = await InvokeApiCore(context, methodSelector).ConfigureAwait(false);
            return response.ResponseContent;
        }
        protected async Task<TContent> InvokeApi<TService, TContent>(HttpTestContext context, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText)
        {
            HttpResponse<TContent> response = await InvokeApiCore(context, methodSelector).ConfigureAwait(false);
            Assert(response, expectedText);
            return response.ResponseContent;
        }

        protected virtual void ConfigureClient(TConfiguration configuration, IHttpClientBuilder builder) { }

        protected virtual Task<IHttpAuthorizationProvider> Authorize(ITestAuthorizationContext context, TConfiguration configuration) => Task.FromResult<IHttpAuthorizationProvider>(null);
        #endregion

        #region Private Methods
        private static Task<TResponse> InvokeApiCore<TService, TResponse>(HttpTestContext context, Expression<Func<TService, Task<TResponse>>> methodSelector)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(context.HttpClientFactory, context.HttpClientOptions, context.HttpAuthorizationProvider);
            return InvokeApi(service, methodSelector);
        }
        private static async Task<TResponse> InvokeApi<TService, TResponse>(TService service, Expression<Func<TService, Task<TResponse>>> methodSelector)
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
                Formatting = Formatting.Indented,
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
            });
            JToken actualTextDom = JToken.Parse(actualText);
            string expectedTextReplaced = Regex.Replace(expectedText, @"\{(?<path>[A-Za-z.]+)\}", x =>
            {
                string path = x.Groups["path"].Value;
                if (!(actualTextDom.SelectToken(path) is JValue value) || value.Value == null)
                    throw new InvalidOperationException($"Replace pattern did not match a JSON path in the actual document: {path} ({x.Index})");

                return value.Value.ToString();
            });
            AssertEqual(expectedTextReplaced, actualText, extension: "json");
        }

        private static HttpTestContext CreateTestContext(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider authorizationProvider)
        {
            return new HttpTestContext(httpClientFactory, httpClientOptions, authorizationProvider);
        }
        #endregion

        #region Nested Types
        private sealed class TestAuthorizationContext : ITestAuthorizationContext
        {
            private readonly IHttpClientFactory _httpClientFactory;
            private readonly HttpClientOptions _httpClientOptions;

            public TestAuthorizationContext(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions)
            {
                _httpClientFactory = httpClientFactory;
                _httpClientOptions = httpClientOptions;
            }

            public TService CreateService<TService>() => HttpServiceFactory.CreateServiceInstance<TService>(_httpClientFactory, _httpClientOptions);
            public TService CreateService<TService>(IHttpAuthorizationProvider authorizationProvider) => HttpServiceFactory.CreateServiceInstance<TService>(_httpClientFactory, _httpClientOptions, authorizationProvider);
        }
        #endregion
    }
}