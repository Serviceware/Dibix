using System;
using Dibix.Sdk.MSBuild;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal sealed class ClientCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ClientContractCSWriter>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.ClientOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.ClientOutputFilePath;
        protected override ClientContractCSWriter CreateGenerator(ISchemaRegistry schemaRegistry, IErrorReporter errorReporter) => new ClientContractCSWriter(errorReporter, schemaRegistry);
    }
}