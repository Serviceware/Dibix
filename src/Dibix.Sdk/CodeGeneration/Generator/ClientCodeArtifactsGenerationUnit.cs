using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeArtifactsGenerationUnit : CodeArtifactsGenerationUnit
    {
        protected override CodeArtifactKind CodeArtifactKind => CodeArtifactKind.Client;
        public override bool ShouldGenerate(CodeArtifactsGenerationContext context) => !String.IsNullOrEmpty(context.ClientOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationContext context) => context.DefaultOutputFilePath;
    }
}