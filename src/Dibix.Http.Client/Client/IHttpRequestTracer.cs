using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public interface IHttpRequestTracer
    {
        /// <summary>
        /// When this is true, the request message will be loaded completely into memory before consuming it for further tracing.
        /// This should be used with care, especially when dealing with large request content to avoid huge memory allocations.
        /// </summary>
        bool CollectRequestContent { get; set; }

        void TraceRequest(HttpRequestMessage requestMessage, string dump);
        void TraceResponse(HttpResponseMessage responseMessage, string dump, TimeSpan duration);
    }
}