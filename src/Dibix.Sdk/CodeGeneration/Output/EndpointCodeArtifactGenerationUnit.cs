using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<EndpointCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => model.Controllers.Any();
        protected override string GetOutputName(CodeGenerationModel model) => model.AreaName;
        protected override EndpointCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) => new EndpointCodeGenerator(model, schemaDefinitionResolver, logger);
    }
}