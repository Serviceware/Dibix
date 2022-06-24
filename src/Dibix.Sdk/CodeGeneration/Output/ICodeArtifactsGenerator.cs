namespace Dibix.Sdk.CodeGeneration
{
    internal interface ICodeArtifactsGenerator
    {
        bool Generate(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger);
    }
}