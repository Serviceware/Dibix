using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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
            HttpClientOptions httpClientOptions = new HttpClientOptions();
            ConfigureOptions(httpClientOptions);
            ITestAuthorizationContext authorizationContext = new TestAuthorizationContext(httpClientFactory, httpClientOptions);
            IHttpAuthorizationProvider authorizationProvider = await Authorize(authorizationContext, Configuration).ConfigureAwait(false);
            TTestContext testContext = contextCreator(httpClientFactory, httpClientOptions, authorizationProvider);
            await testFlow(testContext).ConfigureAwait(false);
        }

        protected virtual void ConfigureOptions(HttpClientOptions options)
        {
            // Do not convert to local time, to ensure timezone agnostic response assertions in tests
            options.ResponseContent.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            
            // We don't want host names in our expected response text used for asserts
            options.ResponseContent.MakeRelativeUrisAbsolute = false;
        }

        protected Task InvokeApi<TService>(HttpTestContext context, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => CreateServiceAndInvokeApi(context, methodSelector);
        protected Task InvokeApi<TService>(TService service, Expression<Func<TService, Task<HttpResponseMessage>>> methodSelector) => InvokeApiCore(service, methodSelector);
        protected async Task<TResponseContent> InvokeApi<TService, TResponseContent>(HttpTestContext context, Expression<Func<TService, Task<HttpResponse<TResponseContent>>>> methodSelector)
        {
            HttpResponse<TResponseContent> response = await CreateServiceAndInvokeApi(context, methodSelector).ConfigureAwait(false);
            return response.ResponseContent;
        }
        protected async Task<TResponseContent> InvokeApi<TService, TResponseContent>(TService service, Expression<Func<TService, Task<HttpResponse<TResponseContent>>>> methodSelector)
        {
            HttpResponse<TResponseContent> response = await InvokeApiCore(service, methodSelector).ConfigureAwait(false);
            return response.ResponseContent;
        }

        protected async Task<TResponseContent> InvokeApiAndAssertResponse<TService, TResponseContent>(TService service, Expression<Func<TService, Task<HttpResponse<TResponseContent>>>> methodSelector, string expectedText = null, string outputName = null, Action<JsonSerializerSettings> configureSerializer = null)
        {
            HttpResponse<TResponseContent> response = await InvokeApiCore(service, methodSelector).ConfigureAwait(false);
            AssertJsonResponse(response.ResponseContent, configureSerializer, outputName, expectedText);
            return response.ResponseContent;
        }
        protected async Task<TResponseContent> InvokeApiAndAssertResponse<TService, TResponseContent>(HttpTestContext context, Expression<Func<TService, Task<HttpResponse<TResponseContent>>>> methodSelector, string expectedText = null, string outputName = null, Action<JsonSerializerSettings> configureSerializer = null)
        {
            HttpResponse<TResponseContent> response = await CreateServiceAndInvokeApi(context, methodSelector).ConfigureAwait(false);
            AssertJsonResponse(response.ResponseContent, configureSerializer, outputName, expectedText);
            return response.ResponseContent;
        }

        protected virtual void ConfigureClient(TConfiguration configuration, IHttpClientBuilder builder) { }

        protected virtual Task<IHttpAuthorizationProvider> Authorize(ITestAuthorizationContext context, TConfiguration configuration) => Task.FromResult<IHttpAuthorizationProvider>(null);
        #endregion

        #region Private Methods
        private static async Task<TResponse> InvokeApiCore<TService, TResponse>(TService service, Expression<Func<TService, Task<TResponse>>> methodSelector)
        {
            Func<TService, Task<TResponse>> compiled = methodSelector.Compile();
            Task<TResponse> task = compiled(service);
            TResponse response = await task.ConfigureAwait(false);
            return response;
        }

        private static Task<TResponse> CreateServiceAndInvokeApi<TService, TResponse>(HttpTestContext context, Expression<Func<TService, Task<TResponse>>> methodSelector)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(context.HttpClientFactory, context.HttpClientOptions, context.HttpAuthorizationProvider);
            return InvokeApiCore(service, methodSelector);
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

            public TService CreateService<TService>() => CreateService<TService>(new EmptyHttpAuthorizationProvider());
            public TService CreateService<TService>(IHttpAuthorizationProvider authorizationProvider) => HttpServiceFactory.CreateServiceInstance<TService>(_httpClientFactory, _httpClientOptions, authorizationProvider);

            public HttpClient CreateClient() => _httpClientFactory.CreateClient(TestHttpClientFactoryBuilder.HttpClientName);
        }

        private sealed class EmptyHttpAuthorizationProvider : IHttpAuthorizationProvider
        {
            public string GetValue(string schemeName) => null;
        }
        #endregion
    }
}