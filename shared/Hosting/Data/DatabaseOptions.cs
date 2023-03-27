namespace Dibix.Hosting.Abstractions.Data
{
    public sealed class DatabaseOptions
    {
        public const string ConfigurationSectionName = "Database";

        public string? ConnectionString { get; set; }
    }
}