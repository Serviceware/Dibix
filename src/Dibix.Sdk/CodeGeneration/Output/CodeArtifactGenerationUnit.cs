using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CodeArtifactGenerationUnit
    {
        public abstract bool ShouldGenerate(CodeGenerationModel model);
        public abstract bool Generate(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger);
    }

    internal abstract class CodeArtifactGenerationUnit<TGenerator> : CodeArtifactGenerationUnit where TGenerator : CodeGenerator
    {
        public override bool Generate(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            TGenerator generator = this.CreateGenerator(model, schemaDefinitionResolver, logger);

            string generated = generator.Generate(model);

            if (!logger.HasLoggedErrors)
            {
                string outputFilePath = Path.Combine(model.OutputDirectory, $"{this.GetOutputName(model)}.cs");
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }

        protected abstract TGenerator CreateGenerator(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger);

        protected abstract string GetOutputName(CodeGenerationModel model);
    }
}