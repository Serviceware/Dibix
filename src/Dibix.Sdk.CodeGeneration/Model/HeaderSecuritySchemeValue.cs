namespace Dibix.Sdk.CodeGeneration
{
    public sealed class HeaderSecuritySchemeValue : SecuritySchemeValue
    {
        public string HeaderName { get; }

        public HeaderSecuritySchemeValue(string headerName)
        {
            HeaderName = headerName;
        }
    }
}