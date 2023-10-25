namespace Dibix.Http.Host
{
    public sealed class AuthenticationOptions
    {
        public const string ConfigurationSectionName = "Authentication";

        public string? Authority { get; set; }
        public string Audience { get; set; } = "dibix";
    }
}