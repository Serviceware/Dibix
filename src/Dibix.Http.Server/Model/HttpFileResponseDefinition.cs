namespace Dibix.Http.Server
{
    public sealed class HttpFileResponseDefinition
    {
        public bool Cache { get; }

        public HttpFileResponseDefinition(bool cache)
        {
            this.Cache = cache;
        }
    }
}