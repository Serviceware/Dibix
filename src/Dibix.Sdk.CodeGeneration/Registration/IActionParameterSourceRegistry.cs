using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IActionParameterSourceRegistry
    {
        internal void Register<TSource>(TSource source, Func<TSource, IActionParameterPropertySourceValidator> validatorFactory) where TSource : ActionParameterSourceDefinition;
        internal bool TryGetDefinition(string sourceName, out ActionParameterSourceDefinition definition);
        internal bool TryGetValidator<TSource>(TSource source, out IActionParameterPropertySourceValidator validator) where TSource : ActionParameterSourceDefinition;
    }
}