using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ServerCodeGenerator>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.DefaultOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.DefaultOutputFilePath;
        protected override ServerCodeGenerator CreateGenerator(ISchemaRegistry schemaRegistry, ILogger logger) => new ServerCodeGenerator(logger, schemaRegistry);
    }
}