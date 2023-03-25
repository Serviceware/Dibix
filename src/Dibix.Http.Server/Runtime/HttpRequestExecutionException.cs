using System;
using System.Net;

namespace Dibix.Http.Server
{
    public sealed class HttpRequestExecutionException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool IsClientError { get; }

        internal HttpRequestExecutionException(HttpStatusCode statusCode, int errorCode, string errorMessage, bool isClientError, Exception innerException) : base($"{(int)statusCode} {statusCode}: {innerException.Message}", innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            IsClientError = isClientError;
        }
    }
}