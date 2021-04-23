using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public sealed class HttpRequestTrace
    {
        public HttpRequestMessage RequestMessage { get; }
        public string FormattedRequestText { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public string FormattedResponseText { get; set; }
        public TimeSpan Duration { get; set; }

        internal HttpRequestTrace(HttpRequestMessage requestMessage, string formattedRequestText)
        {
            this.RequestMessage = requestMessage;
            this.FormattedRequestText = formattedRequestText;
        }
    }
}