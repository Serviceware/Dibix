using System;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Validations;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal abstract class ValidationRuleDescriptor<T> : ValidationRuleDescriptor where T : IOpenApiElement
    {
        public sealed override ValidationRule Create() => new ValidationRule<T>(this.Validate);

        protected abstract void Validate(IValidationContext context, T target);
    }

    internal abstract class ValidationRuleDescriptor
    {
        private readonly string _ruleName;

        protected abstract string ErrorMessage { get; }

        protected ValidationRuleDescriptor() => this._ruleName = this.GetRuleName();

        public abstract ValidationRule Create();

        protected virtual string GetRuleName()
        {
            string name = this.GetType().Name;
            int suffix = name.LastIndexOf("ValidationRule", StringComparison.Ordinal);
            if (suffix >= 0)
                name = name.Substring(0, suffix);

            return name;
        }

        protected void AddError(IValidationContext context) => context.CreateError(this._ruleName, this.ErrorMessage);
    }
}