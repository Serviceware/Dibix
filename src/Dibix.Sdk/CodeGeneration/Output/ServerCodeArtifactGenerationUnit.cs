using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ServerCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => true;
        protected override string GetOutputName(CodeGenerationModel model) => $"{model.ArtifactGenerationConfiguration.DefaultOutputName}.Accessor";
        protected override ServerCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) => new ServerCodeGenerator(model, schemaDefinitionResolver, logger);
    }
}