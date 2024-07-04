namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecurityScheme
    {
        public string SchemeName { get; }
        public SecuritySchemeValue Value { get; }

        public SecurityScheme(string schemeName, SecuritySchemeValue value)
        {
            SchemeName = schemeName;
            Value = value;
        }
    }
}