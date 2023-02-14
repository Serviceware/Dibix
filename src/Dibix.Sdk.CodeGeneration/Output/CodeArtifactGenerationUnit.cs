using System.IO;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CodeArtifactGenerationUnit
    {
        public abstract bool ShouldGenerate(CodeGenerationModel model);
        public abstract bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);
    }

    internal abstract class CodeArtifactGenerationUnit<TGenerator> : CodeArtifactGenerationUnit where TGenerator : CodeGenerator
    {
        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TGenerator generator = CreateGenerator(model, schemaRegistry, logger);

            string generated = generator.Generate(model);

            if (!logger.HasLoggedErrors)
            {
                string outputFilePath = Path.Combine(model.OutputDirectory, $"{GetOutputName(model)}.cs");
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }

        protected abstract TGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger);

        protected abstract string GetOutputName(CodeGenerationModel model);
    }
}