using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;
using Newtonsoft.Json;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TService, TConfiguration> : WebApiTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new() where TService : IHttpService
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<HttpTestContext<TService>, Task> testFlow) => base.ExecuteTest(testFlow, CreateTestContext);

        protected Task ExecuteTest<TContent>(Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector) => ExecuteTest(x => InvokeApiAndAssertResponse(x.Service, methodSelector));

        protected Task<TContent> InvokeApi<TContent>(HttpTestContext<TService> context, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector) => InvokeApi(context.Service, methodSelector);

        protected Task<TContent> InvokeApiAndAssertResponse<TContent>(HttpTestContext<TService> context, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector, string expectedText = null, string outputName = null, Action<JsonSerializerSettings> configureSerializer = null) => InvokeApiAndAssertResponse(context.Service, methodSelector, expectedText, outputName, configureSerializer);
        #endregion

        #region Private Methods
        private static HttpTestContext<TService> CreateTestContext(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider authorizationProvider)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(httpClientFactory, httpClientOptions, authorizationProvider);
            return new HttpTestContext<TService>(service, httpClientFactory, httpClientOptions, authorizationProvider);
        }
        #endregion
    }
}