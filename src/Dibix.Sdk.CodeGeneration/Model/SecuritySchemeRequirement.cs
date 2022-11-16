namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecuritySchemeRequirement
    {
        public SecurityScheme Scheme { get; }

        public SecuritySchemeRequirement(SecurityScheme scheme)
        {
            Scheme = scheme;
        }
    }
}