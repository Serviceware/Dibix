using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerator : ICodeArtifactsGenerator
    {
        private static readonly Type[] Units =
        {
            typeof(ServerCodeArtifactGenerationUnit)
          , typeof(ClientCodeArtifactGenerationUnit)
          , typeof(OpenApiArtifactsGenerationUnit)
          , typeof(DumpModelUnit)
        };

        public bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            bool failed = false;
            foreach (Type unitType in Units)
            {
                CodeArtifactGenerationUnit unit = (CodeArtifactGenerationUnit)Activator.CreateInstance(unitType);
                if (!unit.ShouldGenerate(model))
                    continue;

                if (!unit.Generate(model, schemaRegistry, logger))
                    failed = true;
            }
            return !failed;
        }
    }
}