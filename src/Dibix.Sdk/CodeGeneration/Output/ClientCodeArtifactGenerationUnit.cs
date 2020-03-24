using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ClientContractCSWriter>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.ClientOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.ClientOutputFilePath;
        protected override ClientContractCSWriter CreateGenerator(ISchemaRegistry schemaRegistry, ILogger logger) => new ClientContractCSWriter(logger, schemaRegistry);
    }
}