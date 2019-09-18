namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ServerCodeArtifactsGenerationUnit : CodeArtifactsGenerationUnit
    {
        protected override CodeArtifactKind CodeArtifactKind => CodeArtifactKind.Server;
        public override bool ShouldGenerate(CodeArtifactsGenerationContext context) => true;
        protected override string GetOutputFilePath(CodeArtifactsGenerationContext context) => context.DefaultOutputFilePath;
    }
}