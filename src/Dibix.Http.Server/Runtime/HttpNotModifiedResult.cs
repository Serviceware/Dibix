namespace Dibix.Http.Server
{
    // Passed to the response formatter, when the procedure short-circuits using 'THROW 304000, @etag, 1'
    public sealed class HttpNotModifiedResult : IETagProvider
    {
        public string ETag { get; }

        internal HttpNotModifiedResult(string etag)
        {
            ETag = etag;
        }
    }
}