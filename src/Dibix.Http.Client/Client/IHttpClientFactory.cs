using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpClientFactory
    {
        HttpClient Create();
    }
}