namespace Dibix.Http.Server
{
    internal sealed class PathParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = PathParameterSource.SourceName;

        public override HttpParameterLocation Location => HttpParameterLocation.Path;
    }
}