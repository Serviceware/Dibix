namespace Dibix.Http
{
    internal sealed class QueryParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "QUERY";
    }
}