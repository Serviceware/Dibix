using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public class HttpRequestTracer
    {
        public bool MaskSensitiveData { get; set; } = true;
        public HttpRequestTrace LastRequest { get; private set; }

        public virtual void TraceRequest(HttpRequestMessage requestMessage, string formattedRequestText)
        {
            this.LastRequest = new HttpRequestTrace(requestMessage, formattedRequestText);
        }

        public virtual void TraceResponse(HttpResponseMessage responseMessage, string formattedResponseText, TimeSpan duration)
        {
            if (this.LastRequest == null)
                throw new InvalidOperationException("Request not initialized");

            this.LastRequest.ResponseMessage = responseMessage;
            this.LastRequest.FormattedResponseText = formattedResponseText;
            this.LastRequest.Duration = duration;
        }
    }
}