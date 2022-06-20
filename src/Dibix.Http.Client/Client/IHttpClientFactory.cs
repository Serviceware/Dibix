using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpClientFactory
    {
        HttpClient CreateClient();
        HttpClient CreateClient(string name);
        HttpClient CreateClient(Uri baseAddress);
        HttpClient CreateClient(string name, Uri baseAddress);
    }
}