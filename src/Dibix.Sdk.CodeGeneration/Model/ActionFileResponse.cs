using Dibix.Http;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionFileResponse
    {
        public string MediaType { get; }
        public bool Cache { get; }
        public ContentDispositionType DispositionType { get; }
        public SourceLocation? IndentJson { get; }

        public ActionFileResponse(string mediaType, bool cache, ContentDispositionType dispositionType, SourceLocation? indentJson)
        {
            MediaType = mediaType;
            Cache = cache;
            DispositionType = dispositionType;
            IndentJson = indentJson;
        }
    }
}