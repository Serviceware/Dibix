using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DumpModelUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => true;

        public override bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string jsonFilePath = Path.ChangeExtension(model.DefaultOutputFilePath, "model.json");
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