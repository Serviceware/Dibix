namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ErrorDescription
    {
        public int ErrorCode { get; }
        public string Description { get; }
        public SourceLocation Location { get; }

        public ErrorDescription(int errorCode, string description, SourceLocation location)
        {
            ErrorCode = errorCode;
            Description = description;
            Location = location;
        }
    }
}