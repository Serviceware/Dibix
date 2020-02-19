using Dibix.Sdk.MSBuild;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal sealed class ServerCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<DaoCodeGenerator>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => true;
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.DefaultOutputFilePath;
        protected override DaoCodeGenerator CreateGenerator(IErrorReporter errorReporter) => new DaoCodeGenerator(errorReporter);
    }
}