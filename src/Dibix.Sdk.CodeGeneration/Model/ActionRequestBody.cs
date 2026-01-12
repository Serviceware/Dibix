namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionRequestBody
    {
        public string MediaType { get; } = HttpMediaType.Json;
        public TypeReference Contract { get; }
        public string Binder { get; }
        public SourceLocation? TreatAsFile { get; }
        public long? MaxContentLength { get; }
        public SourceLocation Location { get; }

        public ActionRequestBody(TypeReference contract, SourceLocation location)
        {
            Contract = contract;
            Location = location;
        }
        public ActionRequestBody(TypeReference contract, SourceLocation location, string mediaType, long? maxContentLength) : this(contract, location)
        {
            MediaType = mediaType;
            MaxContentLength = maxContentLength;
        }
        public ActionRequestBody(TypeReference contract, SourceLocation location, string mediaType, string binder, SourceLocation? treatAsFile, long? maxContentLength) : this(contract, location, mediaType, maxContentLength)
        {
            Binder = binder;
            TreatAsFile = treatAsFile;
        }
    }
}