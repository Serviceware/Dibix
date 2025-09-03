namespace Dibix.Http
{
    public sealed class HttpFileResponseDefinition
    {
        public bool Cache { get; }
        public ContentDispositionType DispositionType { get; }

        public HttpFileResponseDefinition(bool cache, ContentDispositionType dispositionType)
        {
            Cache = cache;
            DispositionType = dispositionType;
        }
    }
}