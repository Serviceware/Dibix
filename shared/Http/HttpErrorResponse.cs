namespace Dibix.Http
{
    public sealed class HttpErrorResponse
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        public HttpErrorResponse(int statusCode, int errorCode, string errorMessage)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}