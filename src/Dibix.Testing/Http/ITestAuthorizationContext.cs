using System.Net.Http;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public interface ITestAuthorizationContext
    {
        TService CreateService<TService>();
        TService CreateService<TService>(IHttpAuthorizationProvider authorizationProvider);
        HttpClient CreateClient();
    }
}