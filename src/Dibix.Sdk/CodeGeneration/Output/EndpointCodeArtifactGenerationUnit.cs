using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<EndpointCodeGenerator>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.EndpointOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.EndpointOutputFilePath;
        protected override EndpointCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new EndpointCodeGenerator(model, logger, schemaRegistry);
    }
}