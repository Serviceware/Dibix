using System;
using System.Net.Http;

namespace Dibix.Http.Server
{
    public sealed class HttpRequestExecutionException : Exception
    {
        public HttpResponseMessage ErrorResponse { get; }
        public bool IsClientError { get; }

        public HttpRequestExecutionException(HttpResponseMessage errorResponse, bool isClientError, Exception innerException) : base($"{(int)errorResponse.StatusCode} {errorResponse.StatusCode}: {innerException.Message}", innerException)
        {
            this.ErrorResponse = errorResponse;
            this.IsClientError = isClientError;
        }
    }
}