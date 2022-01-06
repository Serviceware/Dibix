using System.Net.Http;

namespace Dibix.Http.Client
{
    public sealed class HttpValidationException : HttpException
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        internal HttpValidationException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText, int errorCode, string errorMessage) : base(request, requestContentText, response, responseContentText)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }
    }
}