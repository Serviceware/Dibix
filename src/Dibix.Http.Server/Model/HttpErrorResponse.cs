namespace Dibix.Http.Server
{
    public readonly struct HttpErrorResponse
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        public HttpErrorResponse(int statusCode, string errorMessage) : this(statusCode, errorCode: 0, errorMessage) { }
        public HttpErrorResponse(int statusCode, int errorCode, string errorMessage)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}