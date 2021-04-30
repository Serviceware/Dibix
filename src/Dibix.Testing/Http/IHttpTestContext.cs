using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public interface IHttpTestContext<out TService, out TConfiguration> : IHttpTestContext<TConfiguration>
    {
        TService Service { get; }
    }

    public interface IHttpTestContext<out TConfiguration>
    {
        TConfiguration Configuration { get; }
        IHttpClientFactory HttpClientFactory { get; }
        IHttpAuthorizationProvider HttpAuthorizationProvider { get; }
    }
}