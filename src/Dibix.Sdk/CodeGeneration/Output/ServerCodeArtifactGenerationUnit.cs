using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ServerCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.DefaultOutputFilePath);
        protected override string GetOutputFilePath(CodeGenerationModel model) => model.DefaultOutputFilePath;
        protected override ServerCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new ServerCodeGenerator(model, logger, schemaRegistry);
    }
}