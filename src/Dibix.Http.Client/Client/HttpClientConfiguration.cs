namespace Dibix.Http.Client
{
    public abstract class HttpClientConfiguration
    {
        public abstract string Name { get; }

        public abstract void Configure(IHttpClientBuilder builder);
    }
}