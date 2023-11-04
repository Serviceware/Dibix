using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ClientCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.ClientTargetFileName);
        protected override string GetOutputName(CodeGenerationModel model) => model.ClientTargetFileName;
        protected override ClientCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new ClientCodeGenerator(model, schemaRegistry, logger);
    }
}