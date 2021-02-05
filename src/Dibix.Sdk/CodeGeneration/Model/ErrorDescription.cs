namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ErrorDescription
    {
        public int ErrorCode { get; }
        public string Description { get; }

        public ErrorDescription(int errorCode, string description)
        {
            this.ErrorCode = errorCode;
            this.Description = description;
        }
    }
}