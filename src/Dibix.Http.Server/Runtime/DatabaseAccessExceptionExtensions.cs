using System.Net;

namespace Dibix.Http.Server
{
    public static class DatabaseAccessExceptionExtensions
    {
        private static readonly string PropertyKey = $"{typeof(HttpRequestExecutionException)}";

        extension(DatabaseAccessException exception)
        {
            public bool IsClientError => GetHttpRequestExecutionException(exception)?.IsClientError ?? false;
            public HttpStatusCode HttpStatusCode => GetHttpRequestExecutionException(exception)?.StatusCode ?? HttpStatusCode.InternalServerError;
            public int ErrorCode => GetHttpRequestExecutionException(exception)?.ErrorCode ?? 0;
            public string ErrorMessage => GetHttpRequestExecutionException(exception)?.ErrorMessage;
        }

        private static HttpRequestExecutionException GetHttpRequestExecutionException(DatabaseAccessException exception)
        {
            HttpRequestExecutionException httpException;
            if (!exception.Data.Contains(PropertyKey))
            {
                httpException = SqlHttpStatusCodeParser.TryParse(exception);
                exception.Data.Add(PropertyKey, httpException);
            }
            else
            {
                object value = exception.Data[PropertyKey];
                httpException = (HttpRequestExecutionException)value;
            }
            return httpException;
        }
    }
}