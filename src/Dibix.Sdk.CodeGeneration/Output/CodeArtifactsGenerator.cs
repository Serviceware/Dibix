using System;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerator : ICodeArtifactsGenerator
    {
        private static readonly Type[] Units =
        {
            typeof(AccessorCodeArtifactGenerationUnit)
          , typeof(EndpointCodeArtifactGenerationUnit)
          , typeof(ClientCodeArtifactGenerationUnit)
          , typeof(OpenApiArtifactsGenerationUnit)
          , typeof(PersistArtifactModelUnit)
          , typeof(PackageMetadataUnit)
        };

        public async Task<bool> Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger)
        {
            bool failed = false;
            foreach (Type unitType in Units)
            {
                CodeArtifactGenerationUnit unit = (CodeArtifactGenerationUnit)Activator.CreateInstance(unitType);
                if (!unit.ShouldGenerate(model))
                    continue;

                if (!await unit.Generate(model, schemaRegistry, actionParameterConverterRegistry, logger).ConfigureAwait(false))
                    failed = true;
            }
            return !failed;
        }
    }
}