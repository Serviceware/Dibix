namespace Dibix.Http
{
    public sealed class HttpFileResponseDefinition
    {
        public bool Cache { get; }
        public ContentDispositionType DispositionType { get; }
        public bool IndentJson { get; }

        public HttpFileResponseDefinition(bool cache, ContentDispositionType dispositionType, bool indentJson)
        {
            Cache = cache;
            DispositionType = dispositionType;
            IndentJson = indentJson;
        }
    }
}