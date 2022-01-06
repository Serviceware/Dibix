using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterSourceRegistry
    {
        void Register<TSource>(TSource source, Func<TSource, IActionParameterPropertySourceValidator> validatorFactory) where TSource : ActionParameterSourceDefinition;
        bool TryGetDefinition(string sourceName, out ActionParameterSourceDefinition definition);
        bool TryGetValidator<TSource>(TSource source, out IActionParameterPropertySourceValidator validator) where TSource : ActionParameterSourceDefinition;
    }
}