using System.Net.Http;

namespace Dibix.Http.Server.AspNet
{
    public static class HttpRequestExecutionExceptionExtensions
    {
        public static HttpResponseMessage CreateResponse(this HttpRequestExecutionException exception, HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse(exception.StatusCode);

            if (exception.IsClientError)
            {
                response.Headers.Add(KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());
                response.Headers.Add(KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
                response.Content = new StringContent(exception.ErrorMessage);
            }

            return response;
        }
    }
}