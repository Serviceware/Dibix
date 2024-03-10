using System.Net;
using System.Net.Http;

namespace Dibix.Http.Server
{
    public sealed class HttpResponse
    {
        public HttpStatusCode StatusCode { get; }

        public HttpResponse(HttpStatusCode statusCode) => StatusCode = statusCode;

        public HttpResponseMessage CreateResponse(HttpRequestMessage request) => request.CreateResponse(this.StatusCode);
    }
}