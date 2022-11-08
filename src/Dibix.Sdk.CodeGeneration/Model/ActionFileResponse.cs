namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionFileResponse
    {
        public bool Cache { get; set; } = true;
        public string MediaType { get; }

        public ActionFileResponse(string mediaType)
        {
            this.MediaType = mediaType;
        }
    }
}