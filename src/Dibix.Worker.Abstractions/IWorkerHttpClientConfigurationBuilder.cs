using System.Net.Http;
using Dibix.Http.Client;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerHttpClientConfigurationBuilder
    {
        void AddTracer<T>() where T : HttpRequestTracer;
        void AddHttpMessageHandler<T>() where T : DelegatingHandler;
    }
}