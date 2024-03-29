﻿using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerator : ICodeArtifactsGenerator
    {
        private static readonly Type[] Units =
        {
            typeof(AccessorCodeArtifactGenerationUnit)
          , typeof(EndpointCodeArtifactGenerationUnit)
          , typeof(ClientCodeArtifactGenerationUnit)
          , typeof(OpenApiArtifactsGenerationUnit)
          , typeof(PersistArtifactModelUnit)
          , typeof(PackageMetadataUnit)
        };

        public bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            bool failed = false;
            foreach (Type unitType in Units)
            {
                CodeArtifactGenerationUnit unit = (CodeArtifactGenerationUnit)Activator.CreateInstance(unitType);
                if (!unit.ShouldGenerate(model))
                    continue;

                if (!unit.Generate(model, schemaRegistry, logger))
                    failed = true;
            }
            return !failed;
        }
    }
}