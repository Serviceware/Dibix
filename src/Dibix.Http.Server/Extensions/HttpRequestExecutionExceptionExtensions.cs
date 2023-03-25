using System.Net.Http;
using System.Text;

namespace Dibix.Http.Server
{
    public static class HttpRequestExecutionExceptionExtensions
    {
        public static HttpResponseMessage CreateResponse(this HttpRequestExecutionException exception, HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse(exception.StatusCode);

            if (exception.IsClientError)
            {
                if (exception.ErrorCode != 0)
                    response.Headers.Add(KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());

                response.Headers.Add(KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
                response.Content = new StringContent($"\"{exception.ErrorMessage}\"", Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}