using System.IO;
using System.Linq;
using Dibix.Sdk.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OpenApiArtifactsGenerationUnit : CodeArtifactsGenerationUnit
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationContext context) => context.Controllers.Any();

        public override bool Generate(CodeArtifactsGenerationContext context)
        {
            string areaName = NamespaceUtility.GetAreaName(context.Namespace);
            OpenApiDocument document = OpenApiGenerator.Generate(context.Namespace, areaName, context.Controllers, context.Contracts);

            if (context.ErrorReporter.HasErrors)
                return false;

            string targetDirectory = Path.GetDirectoryName(context.DefaultOutputFilePath);

            string jsonFilePath = Path.Combine(targetDirectory, $"{areaName}.json");
            using (Stream stream = File.OpenWrite(jsonFilePath))
            {
                document.SerializeAsJson(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            string yamlFilePath = Path.Combine(targetDirectory, $"{areaName}.yml");
            using (Stream stream = File.OpenWrite(yamlFilePath))
            {
                document.SerializeAsYaml(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            return true;
        }
    }
}