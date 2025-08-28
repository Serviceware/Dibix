namespace Dibix.Http
{
    public sealed class HttpFileResponseDefinition
    {
        public bool Cache { get; }

        public HttpFileResponseDefinition(bool cache)
        {
            Cache = cache;
        }
    }
}