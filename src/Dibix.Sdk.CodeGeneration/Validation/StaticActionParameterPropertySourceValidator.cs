namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class StaticActionParameterPropertySourceValidator<TSource> : ActionParameterPropertySourceValidator<TSource>, IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition<TSource>, new()
    {
        protected override TSource Definition { get; }

        protected StaticActionParameterPropertySourceValidator(TSource definition)
        {
            Definition = definition;
        }
    }
}