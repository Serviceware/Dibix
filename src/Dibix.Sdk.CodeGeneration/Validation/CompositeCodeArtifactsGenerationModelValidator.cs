using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CompositeCodeGenerationModelValidator : ICodeGenerationModelValidator
    {
        private readonly ICollection<ICodeGenerationModelValidator> _validators;

        public CompositeCodeGenerationModelValidator(params ICodeGenerationModelValidator[] validators) => this._validators = validators;

        public bool Validate(CodeGenerationModel model)
        {
            bool result = true;
            foreach (ICodeGenerationModelValidator validator in this._validators)
            {
                if (!validator.Validate(model))
                    result = false;
            }
            return result;
        }
    }
}