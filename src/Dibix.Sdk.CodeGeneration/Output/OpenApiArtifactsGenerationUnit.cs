using System;
using System.IO;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OpenApiArtifactsGenerationUnit : OpenApiArtifactsGenerationUnitBase
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.DocumentationTargetName);

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (logger.HasLoggedErrors)
                return false;

            OpenApiDocument document = OpenApiGenerator.Generate(model, schemaRegistry, logger);

            string jsonFilePath = BuildOutputPath(model, "json");
            using (Stream stream = File.Open(jsonFilePath, FileMode.Create))
            {
                document.SerializeAsJson(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            string yamlFilePath = BuildOutputPath(model, "yml");
            using (Stream stream = File.Open(yamlFilePath, FileMode.Create))
            {
                WriteYaml(stream, document);
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
    }
}