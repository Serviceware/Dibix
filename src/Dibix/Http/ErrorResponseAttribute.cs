using System;

namespace Dibix.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ErrorResponseAttribute : Attribute
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorDescription { get; }

        public ErrorResponseAttribute(int statusCode, int errorCode, string errorDescription)
        {
            this.StatusCode = statusCode;
            this.ErrorCode = errorCode;
            this.ErrorDescription = errorDescription;
        }
    }
}