namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CodeArtifactsGenerationUnit
    {
        public abstract bool ShouldGenerate(CodeArtifactsGenerationContext context);
        public abstract bool Generate(CodeArtifactsGenerationContext context);
    }
}