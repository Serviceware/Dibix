using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public interface IHttpTestContext<out TService> : IHttpTestContext where TService : IHttpService
    {
        TService Service { get; }
    }

    public interface IHttpTestContext
    {
        IHttpClientFactory HttpClientFactory { get; }
        IHttpAuthorizationProvider HttpAuthorizationProvider { get; }
    }
}