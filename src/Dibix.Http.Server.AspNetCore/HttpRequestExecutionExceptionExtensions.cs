namespace Dibix.Http.Server.AspNetCore
{
    public static class HttpRequestExecutionExceptionExtensions
    {
        public static void AppendToResponse(this HttpRequestExecutionException exception, Microsoft.AspNetCore.Http.HttpResponse response)
        {
            if (!exception.IsClientError)
                return;

            // Dibix.Http.Host uses ProblemDetails and IExceptionHandler
            // This remains just for compatibility
            Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.Append(response.Headers, KnownHeaders.ClientErrorCodeHeaderName, exception.ErrorCode.ToString());
            Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.Append(response.Headers, KnownHeaders.ClientErrorDescriptionHeaderName, exception.ErrorMessage);
        }
    }
}