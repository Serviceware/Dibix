using System;
using System.IO;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PersistArtifactModelUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.ModelTargetFileName);

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string jsonFilePath = Path.GetFullPath(Path.Combine(model.OutputDirectory, model.ModelTargetFileName));
            CodeGenerationModelSerializer.Write(model, jsonFilePath);
            return true;
        }
    }
}