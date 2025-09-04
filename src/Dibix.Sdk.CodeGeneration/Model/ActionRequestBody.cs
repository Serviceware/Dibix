namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionRequestBody
    {
        public string MediaType { get; } = HttpMediaType.Json;
        public TypeReference Contract { get; }
        public string Binder { get; }
        public SourceLocation? TreatAsFile { get; }
        public SourceLocation Location { get; }

        public ActionRequestBody(TypeReference contract, SourceLocation location)
        {
            Contract = contract;
            Location = location;
        }
        public ActionRequestBody(TypeReference contract, SourceLocation location, string mediaType) : this(contract, location)
        {
            MediaType = mediaType;
        }
        public ActionRequestBody(TypeReference contract, SourceLocation location, string mediaType, string binder, SourceLocation? treatAsFile) : this(contract, location, mediaType)
        {
            Binder = binder;
            TreatAsFile = treatAsFile;
        }
    }
}