using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ServerCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => true;
        protected override string GetOutputName(CodeGenerationModel model) => $"{model.DefaultOutputName}.Accessor";
        protected override ServerCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new ServerCodeGenerator(model, schemaRegistry, logger);
    }
}