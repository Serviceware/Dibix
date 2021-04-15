using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public class HttpRequestTracer : IHttpRequestTracer
    {
        public bool CollectRequestContent { get; set; } = true;
        public HttpRequestTrace LastRequest { get; private set; }

        public virtual void TraceRequest(HttpRequestMessage requestMessage, string dump)
        {
            this.LastRequest = new HttpRequestTrace(requestMessage, dump);
        }

        public virtual void TraceResponse(HttpResponseMessage responseMessage, string dump, TimeSpan duration)
        {
            if (this.LastRequest == null)
                throw new InvalidOperationException("Request not initialized");

            this.LastRequest.ResponseMessage = responseMessage;
            this.LastRequest.ResponseDump = dump;
            this.LastRequest.Duration = duration;
        }
    }
}