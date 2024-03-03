namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class RequestPropertySourceValidator : StaticActionParameterFixedPropertySourceValidator<RequestParameterSource>, IActionParameterPropertySourceValidator
    {
        public RequestPropertySourceValidator(RequestParameterSource definition) : base(definition) { }
    }
}