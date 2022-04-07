using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PersistArtifactModelUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !model.Controllers.Any(); // Only for non-leaf projects

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string jsonFilePath = Path.GetFullPath(Path.Combine(model.OutputDirectory, $"{model.DefaultOutputName}.Accessor.model.json"));
            string serializedModelJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = { new StringEnumConverter() }
            });
            File.WriteAllText(jsonFilePath, serializedModelJson);
            return true;
        }
    }
}