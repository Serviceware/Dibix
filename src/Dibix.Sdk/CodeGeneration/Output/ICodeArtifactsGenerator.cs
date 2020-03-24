namespace Dibix.Sdk.CodeGeneration
{
    internal interface ICodeArtifactsGenerator
    {
        bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);
    }
}