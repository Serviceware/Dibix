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

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TConfiguration> : DatabaseTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<HttpTestContext, Task> testFlow) => this.ExecuteTest(testFlow, CreateTestContext);

        private protected async Task ExecuteTest<TTestContext>(Func<TTestContext, Task> testFlow, Func<IHttpClientFactory, IHttpAuthorizationProvider, TTestContext> contextCreator) where TTestContext : HttpTestContext
        {
            HttpClientConfiguration clientSetup = new TestHttpClientConfiguration(base.TestContext, base.Out, x => this.ConfigureClient(base.Configuration, x));
            IHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(clientSetup);
            ITestAuthorizationContext authorizationContext = new TestAuthorizationContext(httpClientFactory);
            IHttpAuthorizationProvider authorizationProvider = await this.Authorize(authorizationContext, base.Configuration).ConfigureAwait(false);
            TTestContext testContext = contextCreator(httpClientFactory, authorizationProvider);
            await testFlow(testContext).ConfigureAwait(false);
        }

        protected Task InvokeApi<TService>(TService service, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => InvokeApi<TService, HttpResponseMessage>(service, methodSelector);
        protected async Task<TContent> InvokeApi<TService, TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText)
        {
            HttpResponse<TContent> response = await InvokeApi(service, methodSelector).ConfigureAwait(false);
            this.Assert(response, expectedText);
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
            this.Assert(response, expectedText);
            return response.ResponseContent;
        }

        protected virtual void ConfigureClient(TConfiguration configuration, IHttpClientBuilder builder) { }

        protected virtual Task<IHttpAuthorizationProvider> Authorize(ITestAuthorizationContext context, TConfiguration configuration) => Task.FromResult<IHttpAuthorizationProvider>(null);
        #endregion

        #region Private Methods
        private static Task<TResponse> InvokeApiCore<TService, TResponse>(HttpTestContext context, Expression<Func<TService, Task<TResponse>>> methodSelector)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(context.HttpClientFactory, context.HttpAuthorizationProvider);
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
            base.AssertEqual(expectedTextReplaced, actualText, extension: "json");
        }

        private static HttpTestContext CreateTestContext(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider)
        {
            return new HttpTestContext(httpClientFactory, authorizationProvider);
        }
        #endregion

        #region Nested Types
        private sealed class TestAuthorizationContext : ITestAuthorizationContext
        {
            private readonly IHttpClientFactory _httpClientFactory;

            public TestAuthorizationContext(IHttpClientFactory httpClientFactory) => this._httpClientFactory = httpClientFactory;

            public TService CreateService<TService>() => HttpServiceFactory.CreateServiceInstance<TService>(this._httpClientFactory);
            public TService CreateService<TService>(IHttpAuthorizationProvider authorizationProvider) => HttpServiceFactory.CreateServiceInstance<TService>(this._httpClientFactory, authorizationProvider);
        }
        #endregion
    }
}