using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterSourceRegistry : IActionParameterSourceRegistry
    {
        private readonly IDictionary<string, ActionParameterSourceDefinition> _sourceMap = new Dictionary<string, ActionParameterSourceDefinition>();
        private readonly IDictionary<ActionParameterSourceDefinition, IActionParameterPropertySourceValidator> _validatorMap = new Dictionary<ActionParameterSourceDefinition, IActionParameterPropertySourceValidator>();

        public ActionParameterSourceRegistry()
        {
            Register<EnvironmentParameterSource, EnvironmentPropertySourceValidator>(x => new EnvironmentPropertySourceValidator(x));
            Register<BodyParameterSource, BodyPropertySourceValidator>(x => new BodyPropertySourceValidator(x));
            Register<ItemParameterSource, ItemPropertySourceValidator>(x => new ItemPropertySourceValidator(x));
            Register<HeaderParameterSource, HeaderPropertySourceValidator>(x => new HeaderPropertySourceValidator(x));
            Register<PathParameterSource, PathPropertySourceValidator>(x => new PathPropertySourceValidator(x));
            Register<QueryParameterSource, QueryPropertySourceValidator>(x => new QueryPropertySourceValidator(x));
            Register<RequestParameterSource, RequestPropertySourceValidator>(x => new RequestPropertySourceValidator(x));
            Register<ClaimParameterSource, ClaimPropertySourceValidator>(x => new ClaimPropertySourceValidator(x));
        }

        void IActionParameterSourceRegistry.Register<TSource>(TSource source, Func<TSource, IActionParameterPropertySourceValidator> validatorFactory)
        {
            RegisterSource(source);
            RegisterValidator(source, validatorFactory(source));
        }

        bool IActionParameterSourceRegistry.TryGetDefinition(string sourceName, out ActionParameterSourceDefinition definition) => this._sourceMap.TryGetValue(sourceName, out definition);

        bool IActionParameterSourceRegistry.TryGetValidator<TSource>(TSource source, out IActionParameterPropertySourceValidator validator) => this._validatorMap.TryGetValue(source, out validator);

        private void Register<TSource, TValidator>(Func<TSource, TValidator> validatorFactory) where TSource : ActionParameterSourceDefinition<TSource>, new() where TValidator : ActionParameterPropertySourceValidator<TSource>
        {
            TSource source = new TSource();
            RegisterSource(source);
            RegisterValidator(source, validatorFactory(source));
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