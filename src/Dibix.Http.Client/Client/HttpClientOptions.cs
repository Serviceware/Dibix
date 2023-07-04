namespace Dibix.Http.Client
{
    public sealed class HttpClientOptions
    {
        public HttpResponseContentOptions ResponseContent { get; } = new HttpResponseContentOptions();

        public static HttpClientOptions Default => new HttpClientOptions();
    }
}