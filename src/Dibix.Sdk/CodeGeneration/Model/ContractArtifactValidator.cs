﻿namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractArtifactValidator : ICodeArtifactsGenerationModelValidator
    {
        private readonly ILogger _logger;

        public ContractArtifactValidator(ILogger logger)
        {
            this._logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool isValid = true;
            foreach (ContractDefinition contractDefinition in model.Contracts)
            {
                // Validate unused contracts
                string contractFullName = contractDefinition.Schema.FullName;
                if (!contractDefinition.HasReferences)
                {
                    isValid = false;
                    this._logger.LogError(null, $"Unused contract definition: {contractFullName}", contractDefinition.FilePath, contractDefinition.Line, contractDefinition.Column);
                }
            }

            return isValid;
        }
    }
}