using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CodeArtifactGenerationUnit
    {
        public abstract bool ShouldGenerate(CodeArtifactsGenerationModel model);
        public abstract bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);
    }

    internal abstract class CodeArtifactGenerationUnit<TGenerator> : CodeArtifactGenerationUnit where TGenerator : CodeGenerator
    {
        public override bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TGenerator generator = this.CreateGenerator(model, schemaRegistry, logger);

            string generated = generator.Generate(model);

            if (!logger.HasLoggedErrors)
            {
                string outputFilePath = this.GetOutputFilePath(model);
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }

        protected abstract TGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);

        protected abstract string GetOutputFilePath(CodeArtifactsGenerationModel model);
    }
}