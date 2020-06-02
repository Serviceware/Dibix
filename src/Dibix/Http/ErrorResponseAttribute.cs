using System;

namespace Dibix.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ErrorResponseAttribute : Attribute
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorDescription { get; }
        public bool IsClientError { get; }

        public ErrorResponseAttribute(int statusCode, int errorCode, string errorDescription, bool isClientError)
        {
            this.StatusCode = statusCode;
            this.ErrorCode = errorCode;
            this.ErrorDescription = errorDescription;
            this.IsClientError = isClientError;
        }
    }
}