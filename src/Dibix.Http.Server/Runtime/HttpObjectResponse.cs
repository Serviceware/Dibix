using System.Net;
using System.Net.Http;

namespace Dibix.Http.Server
{
    public sealed class HttpObjectResponse : HttpResponse
    {
        public object Content { get; }

        public HttpObjectResponse(HttpStatusCode statusCode, object content) : base(statusCode) => Content = content;

        public override HttpResponseMessage CreateResponse(HttpRequestMessage request) => request.CreateResponse(StatusCode, Content);
    }
}