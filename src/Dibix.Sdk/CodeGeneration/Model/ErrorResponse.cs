namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ErrorResponse
    {
        public int StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorDescription { get; set; }

        public ErrorResponse(int statusCode, int errorCode, string errorDescription)
        {
            this.StatusCode = statusCode;
            this.ErrorCode = errorCode;
            this.ErrorDescription = errorDescription;
        }
    }
}