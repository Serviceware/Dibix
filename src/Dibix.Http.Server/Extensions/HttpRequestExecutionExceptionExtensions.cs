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
                response.Headers.Add(KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());
                response.Headers.Add(KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
                response.Content = new StringContent(exception.ErrorMessage);
            }

            return response;
        }

#if NET
        public static void AppendToResponse(this HttpRequestExecutionException exception, Microsoft.AspNetCore.Http.HttpResponse response)
        {
            if (!exception.IsClientError) 
                return;

            // Dibix.Http.Host uses ProblemDetails and IExceptionHandler
            // This remains just for compatibility
            Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.Append(response.Headers, KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());
            Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.Append(response.Headers, KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
        }
#endif
    }
}