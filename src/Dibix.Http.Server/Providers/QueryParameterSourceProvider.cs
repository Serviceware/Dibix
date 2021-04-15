namespace Dibix.Http.Server
{
    internal sealed class QueryParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "QUERY";
    }
}