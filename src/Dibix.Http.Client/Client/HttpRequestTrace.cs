using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    public sealed class HttpRequestTrace
    {
        public HttpRequestMessage RequestMessage { get; }
        public string RequestDump { get; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public string ResponseDump { get; set; }
        public TimeSpan Duration { get; set; }

        internal HttpRequestTrace(HttpRequestMessage requestMessage, string requestDump)
        {
            this.RequestMessage = requestMessage;
            this.RequestDump = requestDump;
        }
    }
}