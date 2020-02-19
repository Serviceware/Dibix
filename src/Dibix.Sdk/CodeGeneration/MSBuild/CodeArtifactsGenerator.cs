using System;
using Dibix.Sdk.MSBuild;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal sealed class CodeArtifactsGenerator : ICodeArtifactsGenerator
    {
        private static readonly Type[] Units =
        {
            typeof(ServerCodeArtifactGenerationUnit)
          , typeof(ClientCodeArtifactGenerationUnit)
          , typeof(OpenApiArtifactsGenerationUnit)
        };

        public bool Generate(CodeArtifactsGenerationModel model, IErrorReporter errorReporter)
        {
            bool failed = false;
            foreach (Type unitType in Units)
            {
                CodeArtifactGenerationUnit unit = (CodeArtifactGenerationUnit)Activator.CreateInstance(unitType);
                if (!unit.ShouldGenerate(model))
                    continue;

                if (!unit.Generate(model, errorReporter))
                    failed = true;
            }
            return !failed;
        }
    }
}