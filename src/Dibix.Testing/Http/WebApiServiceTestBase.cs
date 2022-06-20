using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TService, TConfiguration> : WebApiTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new() where TService : IHttpService
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<HttpTestContext<TService>, Task> testFlow) => base.ExecuteTest(testFlow, CreateTestContext);

        protected Task ExecuteTest<TContent>(Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector) => this.ExecuteTest(x => this.InvokeApi(x.Service, methodSelector));
        #endregion

        #region Private Methods
        private static HttpTestContext<TService> CreateTestContext(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider)
        {
            TService service = HttpServiceFactory.CreateServiceInstance<TService>(httpClientFactory, authorizationProvider);
            return new HttpTestContext<TService>(service, httpClientFactory, authorizationProvider);
        }

        private Task<TContent> InvokeApi<TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector)
        {
            string expectedText = this.ResolveExpectedTextFromEmbeddedResource();
            return this.InvokeApi(service, methodSelector, expectedText);
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