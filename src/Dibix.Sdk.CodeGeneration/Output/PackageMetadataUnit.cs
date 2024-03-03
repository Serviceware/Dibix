using System;
using System.IO;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PackageMetadataUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.PackageMetadataTargetFileName);

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string packageMetadataPath = Path.GetFullPath(Path.Combine(model.OutputDirectory, model.PackageMetadataTargetFileName));
            ArtifactPackageMetadata metadata = CollectMetadata();
            JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
            using Stream stream = File.Open(packageMetadataPath, FileMode.Create);
            using TextWriter textWriter = new StreamWriter(stream);
            serializer.Serialize(textWriter, metadata);
            return true;
        }

        private static ArtifactPackageMetadata CollectMetadata()
        {
            ArtifactPackageMetadata metadata = new ArtifactPackageMetadata();
            return metadata;
        }
    }
}