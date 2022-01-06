namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DynamicPropertyParameterSourceValidator : ActionParameterFixedPropertySourceValidator<DynamicPropertyParameterSource>, IActionParameterPropertySourceValidator
    {
        protected override DynamicPropertyParameterSource Definition { get; }

        public DynamicPropertyParameterSourceValidator(DynamicPropertyParameterSource definition)
        {
            this.Definition = definition;
        }
    }
}