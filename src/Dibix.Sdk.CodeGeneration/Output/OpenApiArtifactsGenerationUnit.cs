using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.OpenApi;
using Microsoft.OpenApi;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OpenApiArtifactsGenerationUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.DocumentationTargetName);

        public override async Task<bool> Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger)
        {
            if (logger.HasLoggedErrors)
                return false;

            if (!Enum.TryParse($"OpenApi{model.OpenApiSchemaVersion?.Replace(".", "_")}", out OpenApiSpecVersion openApiSpecVersion))
            {
                logger.LogError($"""
                                 Unexpected OpenAPI schema version: {model.OpenApiSchemaVersion}
                                 Supported versions are: {String.Join(", ", Enum.GetValues(typeof(OpenApiSpecVersion)).Cast<OpenApiSpecVersion>().Select(x => x.ToString().Replace("OpenApi", "").Replace('_', '.')))}
                                 """, source: model.ProjectPath, line: 0, column: 0);
            }

            OpenApiDocument document = OpenApiGenerator.Generate(model, schemaRegistry, logger);

            string jsonFilePath = BuildOutputPath(model.OutputDirectory, model.DocumentationTargetName, "json");
            using (Stream stream = File.Open(jsonFilePath, FileMode.Create))
            {
                await document.SerializeAsJsonAsync(stream, openApiSpecVersion).ConfigureAwait(false);
            }

            string yamlFilePath = BuildOutputPath(model.OutputDirectory, model.DocumentationTargetName, "yml");
            using (Stream stream = File.Open(yamlFilePath, FileMode.Create))
            {
                await document.SerializeAsYamlAsync(stream, openApiSpecVersion).ConfigureAwait(false);
            }

            // Unfortunately the validation of the Microsoft SDK is not as thorough as the one on https://editor.swagger.io
            // To catch all errors, like referencing missing schemas, we use swagger manually for now.
            Lazy<JToken> jsonAccessor = new Lazy<JToken>(() => JsonUtility.LoadJson(jsonFilePath));
            foreach (OpenApiError error in OpenApiValidationAdapter.Validate(document, ValidationRules.All))
            {
                JToken json = jsonAccessor.Value;
                OpenApiValidationUtility.LogError(error, jsonFilePath, json, logger);
            }

            return !logger.HasLoggedErrors;
        }

        private static string BuildOutputPath(string targetDirectory, string areaName, string extension) => Path.GetFullPath(Path.Combine(targetDirectory, $"{areaName}.{extension}"));
    }
}