using System;
using System.IO;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PersistArtifactModelUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.ModelTargetFileName);

        public override Task<bool> Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger)
        {
            string jsonFilePath = Path.GetFullPath(Path.Combine(model.OutputDirectory, model.ModelTargetFileName));
            CodeGenerationModelSerializer.Write(model, jsonFilePath);
            return Task.FromResult(true);
        }
    }
}