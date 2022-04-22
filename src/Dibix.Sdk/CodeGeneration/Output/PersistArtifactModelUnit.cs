using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PersistArtifactModelUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !model.Controllers.Any(); // Only for non-leaf projects

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string jsonFilePath = Path.GetFullPath(Path.Combine(model.OutputDirectory, $"{model.DefaultOutputName}.Accessor.model.json"));
            CodeGenerationModelSerializer.Write(model, jsonFilePath);
            return true;
        }
    }
}