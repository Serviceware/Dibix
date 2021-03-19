namespace Dibix.Http
{
    internal sealed class PathParameterSourceProvider : ArgumentsSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "PATH";
    }
}