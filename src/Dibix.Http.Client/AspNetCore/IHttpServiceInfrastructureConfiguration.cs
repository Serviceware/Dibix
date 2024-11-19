using System;

namespace Dibix.Http.Client
{
    public interface IHttpServiceInfrastructureConfiguration
    {
        IHttpServiceInfrastructureConfiguration WithAuthorizationProvider<TAuthorizationProvider>() where TAuthorizationProvider : class, IHttpAuthorizationProvider; 
        void Configure(Action<HttpClientOptions> configure);
    }
}