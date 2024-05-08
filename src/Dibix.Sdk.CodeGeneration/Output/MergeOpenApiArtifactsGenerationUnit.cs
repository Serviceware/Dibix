using System;
using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.Abstractions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class MergeOpenApiArtifactsGenerationUnit : OpenApiArtifactsGenerationUnitBase
    {
        public override bool ShouldGenerate(CodeGenerationModel model)
        {
            return !String.IsNullOrEmpty(model.DocumentationTargetName) && !String.IsNullOrEmpty(model.DocumentationSourcePath) && File.Exists(model.DocumentationSourcePath);
        }

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (logger.HasLoggedErrors)
                return false;

            logger.LogMessage("Merging OpenAPI schemas");
            string existingYamlFilePath = model.DocumentationSourcePath;
            string generatedYamlFilePath = BuildOutputPath(model, "yml");

            OpenApiDocument existingDocument = LoadDocument(existingYamlFilePath);
            OpenApiDocument generatedDocument = LoadDocument(generatedYamlFilePath);

            OpenApiMergeVisitor visitor = new OpenApiMergeVisitor(generatedDocument);
            OpenApiWalker walker = new OpenApiWalker(visitor);
            walker.Walk(existingDocument);

            using Stream stream = File.Open(generatedYamlFilePath, FileMode.Create);
            WriteYaml(stream, generatedDocument);

            return !logger.HasLoggedErrors;
        }

        private static OpenApiDocument LoadDocument(string path)
        {
            using Stream stream = File.OpenRead(path);
            using TextReader reader = new StreamReader(stream);
            return new OpenApiTextReaderReader().Read(reader, out _);
        }

        private sealed class OpenApiMergeVisitor(OpenApiDocument target) : OpenApiVisitorBase
        {
            public override void Visit(OpenApiPathItem pathItem)
            {
                string key = CurrentKeys.Path;
                if (!target.Paths.ContainsKey(key))
                {
                    target.Paths.Add(key, pathItem);
                }
                else
                {
                    foreach (KeyValuePair<OperationType, OpenApiOperation> operation in pathItem.Operations)
                    {
                        IDictionary<OperationType, OpenApiOperation> targetOperations = target.Paths[key].Operations;
                        if (!targetOperations.ContainsKey(operation.Key))
                        {
                            targetOperations.Add(operation);
                        }
                    }
                }
            }

            public override void Visit(OpenApiComponents components)
            {
                foreach (KeyValuePair<string, OpenApiSchema> schema in components.Schemas)
                {
                    IDictionary<string, OpenApiSchema> targetSchemas = target.Components.Schemas;
                    if (!targetSchemas.ContainsKey(schema.Key))
                    {
                        targetSchemas.Add(schema);
                    }
                }
            }
        }
    }
}