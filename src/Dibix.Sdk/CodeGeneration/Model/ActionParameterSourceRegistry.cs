using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterSourceRegistry : IActionParameterSourceRegistry
    {
        private readonly IDictionary<string, ActionParameterSourceDefinition> _sourceMap = new Dictionary<string, ActionParameterSourceDefinition>();
        private readonly IDictionary<ActionParameterSourceDefinition, IActionParameterPropertySourceValidator> _validatorMap = new Dictionary<ActionParameterSourceDefinition, IActionParameterPropertySourceValidator>();

        public ActionParameterSourceRegistry()
        {
            Register<EnvironmentParameterSource, EnvironmentPropertySourceValidator>();
            Register<BodyParameterSource, BodyPropertySourceValidator>();
            Register<ItemParameterSource, ItemPropertySourceValidator>();
            Register<HeaderParameterSource, HeaderPropertySourceValidator>();
            Register<PathParameterSource, PathPropertySourceValidator>();
            Register<QueryParameterSource, QueryPropertySourceValidator>();
            Register<RequestParameterSource, RequestPropertySourceValidator>();
        }

        public void Register<TSource>(TSource source, Func<TSource, IActionParameterPropertySourceValidator> validatorFactory) where TSource : ActionParameterSourceDefinition
        {
            RegisterSource(source);
            RegisterValidator(source, validatorFactory(source));
        }

        public bool TryGetDefinition(string sourceName, out ActionParameterSourceDefinition definition) => this._sourceMap.TryGetValue(sourceName, out definition);

        public bool TryGetValidator<TSource>(TSource source, out IActionParameterPropertySourceValidator validator) where TSource : ActionParameterSourceDefinition => this._validatorMap.TryGetValue(source, out validator);

        private void Register<TSource, TValidator>() where TSource : ActionParameterSourceDefinition<TSource>, new() where TValidator : ActionParameterPropertySourceValidator<TSource>, new()
        {
            TSource source = ActionParameterSourceDefinition<TSource>.Instance;
            RegisterSource(source);
            RegisterValidator(source, new TValidator());
        }

        private void RegisterSource(ActionParameterSourceDefinition source)
        {
            string sourceName = source.Name;
            if (this._sourceMap.TryGetValue(sourceName, out ActionParameterSourceDefinition existingSource))
                throw new InvalidOperationException($"A source with the name '{sourceName}' is already registered: {existingSource.GetType()}");

            this._sourceMap.Add(sourceName, source);
        }
        private void RegisterValidator(ActionParameterSourceDefinition source, IActionParameterPropertySourceValidator validator)
        {
            if (this._validatorMap.TryGetValue(source, out IActionParameterPropertySourceValidator existingValidator))
                throw new InvalidOperationException($"A validator for the source '{CreateSourceDebugInfo(source)}' is already registered: {existingValidator.GetType()}");

            this._validatorMap.Add(source, validator);
        }

        private static string CreateSourceDebugInfo(ActionParameterSourceDefinition source) => $"{source.Name} ({source.GetType()})";
    }
}