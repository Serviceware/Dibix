using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<DaoCodeGenerator>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.DefaultOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.DefaultOutputFilePath;
        protected override DaoCodeGenerator CreateGenerator(ISchemaRegistry schemaRegistry, ILogger logger) => new DaoCodeGenerator(logger, schemaRegistry);
    }
}