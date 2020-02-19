using System.IO;
using System.Linq;
using Dibix.Sdk.MSBuild;
using Dibix.Sdk.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal sealed class OpenApiArtifactsGenerationUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => model.Controllers.Any();

        public override bool Generate(CodeArtifactsGenerationModel model, IErrorReporter errorReporter)
        {
            OpenApiDocument document = OpenApiGenerator.Generate(model.ProductName, NamespaceUtility.EnsureAreaName(model.AreaName), model.Controllers, model.Contracts);

            if (errorReporter.HasErrors)
                return false;

            string targetDirectory = Path.GetDirectoryName(model.DefaultOutputFilePath);

            string jsonFilePath = Path.Combine(targetDirectory, $"{model.AreaName}.json");
            using (Stream stream = File.Open(jsonFilePath, FileMode.Create))
            {
                document.SerializeAsJson(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            string yamlFilePath = Path.Combine(targetDirectory, $"{model.AreaName}.yml");
            using (Stream stream = File.Open(yamlFilePath, FileMode.Create))
            {
                document.SerializeAsYaml(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            return true;
        }
    }
}