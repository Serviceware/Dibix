namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ErrorResponse
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorDescription { get; set; }
        public bool IsClientError { get; }

        public ErrorResponse(int statusCode, int errorCode, string errorDescription, bool isClientError)
        {
            this.StatusCode = statusCode;
            this.ErrorCode = errorCode;
            this.ErrorDescription = errorDescription;
            this.IsClientError = isClientError;
        }
    }
}