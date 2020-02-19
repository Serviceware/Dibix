﻿using System.IO;
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
            OpenApiDocument document = OpenApiGenerator.Generate(context.ProductName, NamespaceUtility.EnsureAreaName(context.AreaName), context.Controllers, context.Contracts);

            if (context.ErrorReporter.HasErrors)
                return false;

            string targetDirectory = Path.GetDirectoryName(context.DefaultOutputFilePath);

            string jsonFilePath = Path.Combine(targetDirectory, $"{context.AreaName}.json");
            using (Stream stream = File.Open(jsonFilePath, FileMode.Create))
            {
                document.SerializeAsJson(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            string yamlFilePath = Path.Combine(targetDirectory, $"{context.AreaName}.yml");
            using (Stream stream = File.Open(yamlFilePath, FileMode.Create))
            {
                document.SerializeAsYaml(stream, OpenApiSpecVersion.OpenApi3_0);
            }

            return true;
        }
    }
}