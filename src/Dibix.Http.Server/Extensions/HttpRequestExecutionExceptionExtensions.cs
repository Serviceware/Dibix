using System.Net.Http;

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
                response.Content = new StringContent(exception.ErrorMessage);
            }

            return response;
        }

#if NET
        public static void AppendToResponse(this HttpRequestExecutionException exception, Microsoft.AspNetCore.Http.HttpResponse response)
        {
            response.StatusCode = (int)exception.StatusCode;

            if (!exception.IsClientError) 
                return;

            if (exception.ErrorCode != 0)
                response.Headers.Add(KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());

            response.Headers.Add(KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
            Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(response, exception.ErrorMessage);
        }
#endif
    }
}