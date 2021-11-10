using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CompositeCodeArtifactsGenerationModelValidator : ICodeArtifactsGenerationModelValidator
    {
        private readonly ICollection<ICodeArtifactsGenerationModelValidator> _validators;

        public CompositeCodeArtifactsGenerationModelValidator(params ICodeArtifactsGenerationModelValidator[] validators) => this._validators = validators;

        public bool Validate(CodeGenerationModel model)
        {
            bool result = true;
            foreach (ICodeArtifactsGenerationModelValidator validator in this._validators)
            {
                if (!validator.Validate(model))
                    result = false;
            }
            return result;
        }
    }
}