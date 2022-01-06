namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class StaticActionParameterFixedPropertySourceValidator<TSource> : ActionParameterFixedPropertySourceValidator<TSource>, IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition<TSource>, IActionParameterFixedPropertySourceDefinition, new()
    {
        protected override TSource Definition => ActionParameterSourceDefinition<TSource>.Instance;
    }
}