using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CodeTextArtifactGenerationUnit : CodeArtifactsGenerationUnit
    {
        protected abstract CodeArtifactKind CodeArtifactKind { get; }

        public override bool Generate(CodeArtifactsGenerationContext context)
        {
            StaticCodeGenerationContext generationContext = new StaticCodeGenerationContext
            (
                context.ProjectDirectory
              , context.Namespace
              , context.Sources
              , context.Contracts
              , context.Controllers
              , context.References
              , this.CodeArtifactKind
              , context.MultipleAreas
              , context.EmbedStatements
              , context.FileSystemProvider
              , context.ContractDefinitionProvider
              , context.ErrorReporter
            );

            ICodeGenerator generator = new DaoCodeGenerator(generationContext);

            string generated = generator.Generate();

            context.DetectedReferences.AddRange(generationContext.Configuration.Output.DetectedReferences);
            if (!context.ErrorReporter.HasErrors)
            {
                string outputFilePath = this.GetOutputFilePath(context);
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }

        protected abstract string GetOutputFilePath(CodeArtifactsGenerationContext context);
    }
}