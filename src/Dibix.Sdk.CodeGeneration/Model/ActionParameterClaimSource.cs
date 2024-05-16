namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterClaimSource : ActionParameterSource, IActionParameterPropertySource
    {
        private readonly ClaimParameterSource _claimParameterSource;

        public ActionParameterSourceDefinition Definition => _claimParameterSource;
        public string PropertyName { get; }
        public SourceLocation Location { get; }
        public string ClaimType => _claimParameterSource.GetClaimTypeName(PropertyName);

        public ActionParameterClaimSource(ClaimParameterSource claimParameterSource, string propertyName, SourceLocation location)
        {
            _claimParameterSource = claimParameterSource;
            PropertyName = propertyName;
            Location = location;
        }
    }
}