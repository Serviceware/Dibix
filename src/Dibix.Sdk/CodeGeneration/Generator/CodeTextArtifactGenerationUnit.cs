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
                context.RootNamespace
              , context.DefaultOutputName
              , context.Configuration
              , context.Statements
              , context.Contracts
              , context.Controllers
              , context.UserDefinedTypes
              , this.CodeArtifactKind
              , context.ContractResolver
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