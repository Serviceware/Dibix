namespace Dibix.Sdk.CodeGeneration
{
    internal interface ICodeArtifactsGenerator
    {
        bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);
    }
}