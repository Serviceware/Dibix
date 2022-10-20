using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ClientCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.ArtifactGenerationConfiguration.ClientOutputName);
        protected override string GetOutputName(CodeGenerationModel model) => model.ArtifactGenerationConfiguration.ClientOutputName;
        protected override ClientCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) => new ClientCodeGenerator(model, schemaDefinitionResolver, logger);
    }
}