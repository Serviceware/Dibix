namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterClaimSource : ActionParameterSource, IActionParameterPropertySource
    {
        private readonly ClaimParameterSource _claimParameterSource;

        public ActionParameterSourceDefinition Definition => _claimParameterSource;
        public string PropertyName { get; }
        public SourceLocation Location { get; }
        public string ClaimType => _claimParameterSource.GetClaimTypeName(PropertyName);
        public override TypeReference Type { get; }

        public ActionParameterClaimSource(ClaimParameterSource claimParameterSource, string propertyName, SourceLocation location)
        {
            _claimParameterSource = claimParameterSource;
            Type = claimParameterSource.TryGetType(propertyName);
            PropertyName = propertyName;
            Location = location;
        }
    }
}