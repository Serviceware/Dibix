using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Dibix.Testing.Data;

namespace Dibix.Testing.Http
{
    public abstract class WebApiTestBase<TService, TConfiguration> : WebApiTestBase<TConfiguration> where TConfiguration : DatabaseConfigurationBase, new()
    {
        #region Protected Methods
        protected virtual Task ExecuteTest(Func<IHttpTestContext<TService, TConfiguration>, Task> testFlow) => base.ExecuteTest(testFlow, CreateTestContext);

        protected Task ExecuteTest<TContent>(Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector) => this.ExecuteTest(x => this.ExecuteTestUnit(x.Service, methodSelector));
        #endregion

        #region Private Methods
        private static IHttpTestContext<TService, TConfiguration> CreateTestContext(TConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider)
        {
            TService service = CreateServiceInstance<TService>(httpClientFactory, authorizationProvider);
            return new HttpServiceTestContext(service, configuration, httpClientFactory, authorizationProvider);
        }

        private Task<TContent> ExecuteTestUnit<TContent>(TService service, Expression<Func<TService, Task<HttpResponse<TContent>>>> methodSelector)
        {
            string expectedText = ResolveExpectedTextFromEmbeddedResource(this.GetType().Assembly, base.TestContext.TestName);
            return this.ExecuteTestUnit(service, methodSelector, expectedText);
        }
        #endregion

        #region Nested Types
        private sealed class HttpServiceTestContext : HttpTestContext, IHttpTestContext<TService, TConfiguration>
        {
            public TService Service { get; }

            public HttpServiceTestContext(TService service, TConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider) : base(configuration, httpClientFactory, httpAuthorizationProvider)
            {
                this.Service = service;
            }
        }
        #endregion
    }
}