namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EnvironmentPropertySourceValidator : StaticActionParameterFixedPropertySourceValidator<EnvironmentParameterSource>, IActionParameterPropertySourceValidator
    {
        public EnvironmentPropertySourceValidator(EnvironmentParameterSource definition) : base(definition) { }
    }
}