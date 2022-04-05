namespace Dibix.Http.Host
{
    internal sealed class CorsOptions
    {
        public const string ConfigurationSectionName = "CORS";

        public string[]? AllowedOrigins { get; set; }
    }
}