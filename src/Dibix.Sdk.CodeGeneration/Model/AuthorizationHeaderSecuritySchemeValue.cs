namespace Dibix.Sdk.CodeGeneration
{
    public class AuthorizationHeaderSecuritySchemeValue : SecuritySchemeValue
    {
        public string Scheme { get; }

        public AuthorizationHeaderSecuritySchemeValue(string scheme)
        {
            Scheme = scheme;
        }
    }
}