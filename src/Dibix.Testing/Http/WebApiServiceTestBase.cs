using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TService, TConfiguration> : WebApiTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new() where TService : IHttpService
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<HttpTestContext<TService>, Task> testFlow) => base.ExecuteTest(testFlow, CreateTestContext);

        protected Task ExecuteTest<TContent>(Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector) => ExecuteTest(x => InvokeApiAndAssertResponse(x.Service, methodSelector));

        protected Task InvokeApiAndAssertResponse<TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector)
        {
            string expectedText = ResolveExpectedTextFromEmbeddedResource();
            return InvokeApi(service, methodSelector, expectedText);
        }
        #endregion

        #region Private Methods
        private static HttpTestContext<TService> CreateTestContext(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider authorizationProvider)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(httpClientFactory, httpClientOptions, authorizationProvider);
            return new HttpTestContext<TService>(service, httpClientFactory, httpClientOptions, authorizationProvider);
        }

        private string ResolveExpectedTextFromEmbeddedResource()
        {
            string resourceKey = $"{base.TestContext.TestName}.json";
            string content = base.GetEmbeddedResourceContent(resourceKey);
            return content;
        }
        #endregion
    }
}