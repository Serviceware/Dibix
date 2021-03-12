namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionRequestBody
    {
        public string MediaType { get; } = HttpMediaType.Default;
        public TypeReference Contract { get; }
        public string Binder { get; }

        public ActionRequestBody(TypeReference contract)
        {
            this.Contract = contract;
        }
        public ActionRequestBody(string mediaType, TypeReference contract) : this(contract)
        {
            this.MediaType = mediaType;
        }
        public ActionRequestBody(string mediaType, TypeReference contract, string binder) : this(mediaType, contract)
        {
            this.Binder = binder;
        }
    }
}