namespace Dibix.Http.Server
{
    internal sealed class PathParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "PATH";

        public override HttpParameterLocation Location => HttpParameterLocation.Path;
    }
}