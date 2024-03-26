using System;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Validations;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal abstract class ValidationRuleDescriptor<T> : ValidationRuleDescriptor where T : IOpenApiElement
    {
        public sealed override ValidationRule Create() => new ValidationRule<T>(RuleName, Validate);

        protected abstract void Validate(IValidationContext context, T target);
    }

    internal abstract class ValidationRuleDescriptor
    {
        private string _ruleName;

        protected string RuleName => _ruleName ??= GetRuleName();
        protected abstract string ErrorMessage { get; }

        public abstract ValidationRule Create();

        protected virtual string GetRuleName()
        {
            string name = GetType().Name;
            int suffix = name.LastIndexOf("ValidationRule", StringComparison.Ordinal);
            if (suffix >= 0)
                name = name.Substring(0, suffix);

            return name;
        }

        protected void AddError(IValidationContext context) => context.CreateError(RuleName, ErrorMessage);
    }
}