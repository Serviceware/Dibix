namespace Dibix.Http.Server
{
    public sealed class HttpParameterClaimSource : HttpParameterSource
    {
        public string ClaimType { get; }
        public override string Description => $"{ClaimParameterSource.SourceName}({ClaimType})";

        internal HttpParameterClaimSource(string claimType)
        {
            ClaimType = claimType;
        }
    }
}