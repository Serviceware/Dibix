using Dibix.Http;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class BearerSecuritySchemeValue() : AuthorizationHeaderSecuritySchemeValue(SecuritySchemeNames.Bearer)
    {
    }
}