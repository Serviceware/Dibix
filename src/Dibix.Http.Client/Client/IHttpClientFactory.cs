using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpClientFactory
    {
        HttpClient CreateClient();
        HttpClient CreateClient(Uri baseAddress);
    }
}