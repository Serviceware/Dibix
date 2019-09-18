using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerator : ICodeArtifactsGenerator
    {
        private static readonly Type[] Units =
        {
            typeof(ServerCodeArtifactsGenerationUnit)
          , typeof(ClientCodeArtifactsGenerationUnit)
        };

        public bool Generate(CodeArtifactsGenerationContext context)
        {
            bool failed = false;
            foreach (Type unitType in Units)
            {
                CodeArtifactsGenerationUnit unit = (CodeArtifactsGenerationUnit)Activator.CreateInstance(unitType);
                if (!unit.ShouldGenerate(context))
                    continue;

                if (!unit.Generate(context))
                    failed = true;
            }
            return !failed;
        }
    }
}