namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterClaimSource : ActionParameterSource
    {
        public string ClaimType { get; }

        public ActionParameterClaimSource(string claimType) => ClaimType = claimType;
    }
}