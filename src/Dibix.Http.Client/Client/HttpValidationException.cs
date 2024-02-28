using System.Net.Http;

namespace Dibix.Http.Client
{
    public sealed class HttpValidationException : HttpException
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        internal HttpValidationException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText, int errorCode, string errorMessage) : base(request, requestContentText, response, responseContentText, CreateExceptionMessage(response, errorCode, errorMessage))
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }

        private static string CreateExceptionMessage(HttpResponseMessage response, int errorCode, string errorMessage)
        {
            string message = @$"{CreateMessage(response)}
{errorMessage} [{errorCode}]";
            return message;
        }
    }
}