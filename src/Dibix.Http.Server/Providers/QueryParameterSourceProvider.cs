namespace Dibix.Http.Server
{
    internal sealed class QueryParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = QueryParameterSource.SourceName;

        public override HttpParameterLocation Location => HttpParameterLocation.Query;
    }
}