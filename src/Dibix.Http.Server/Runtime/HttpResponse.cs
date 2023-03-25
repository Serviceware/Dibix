using System.Net;
using System.Net.Http;

namespace Dibix.Http.Server
{
    public class HttpResponse
    {
        public HttpStatusCode StatusCode { get; }

        public HttpResponse(HttpStatusCode statusCode) => StatusCode = statusCode;

        public virtual HttpResponseMessage CreateResponse(HttpRequestMessage request) => request.CreateResponse(this.StatusCode);
    }
}