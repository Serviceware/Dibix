using System.Net.Http;

namespace Dibix.Http.Client
{
    public sealed class HttpResponse<T>
    {
        public HttpResponseMessage ResponseMessage { get; }
        public T ResponseContent { get; }

        public HttpResponse(HttpResponseMessage responseMessage, T responseContent)
        {
            this.ResponseMessage = responseMessage;
            this.ResponseContent = responseContent;
        }
    }
}