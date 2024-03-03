namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClaimPropertySourceValidator : StaticActionParameterFixedPropertySourceValidator<ClaimParameterSource>, IActionParameterPropertySourceValidator
    {
        public ClaimPropertySourceValidator(ClaimParameterSource definition) : base(definition) { }
    }
}