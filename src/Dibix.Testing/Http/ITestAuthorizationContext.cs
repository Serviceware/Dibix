using System.Net.Http;

namespace Dibix.Testing.Http
{
    public interface ITestAuthorizationContext
    {
        TService CreateService<TService>();
        HttpClient CreateClient();
    }
}