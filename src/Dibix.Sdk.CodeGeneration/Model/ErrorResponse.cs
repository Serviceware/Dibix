namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ErrorResponse
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorDescription { get; }
        public SourceLocation SourceLocation { get; }

        public ErrorResponse(int statusCode, int errorCode, string errorDescription, SourceLocation sourceLocation)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ErrorDescription = errorDescription;
            SourceLocation = sourceLocation;
        }
    }
}