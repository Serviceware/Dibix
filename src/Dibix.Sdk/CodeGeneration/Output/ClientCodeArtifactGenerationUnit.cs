﻿using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<ClientCodeGenerator>
    {
        public override bool ShouldGenerate(CodeArtifactsGenerationModel model) => !String.IsNullOrEmpty(model.ClientOutputFilePath);
        protected override string GetOutputFilePath(CodeArtifactsGenerationModel model) => model.ClientOutputFilePath;
        protected override ClientCodeGenerator CreateGenerator(ISchemaRegistry schemaRegistry, ILogger logger) => new ClientCodeGenerator(logger, schemaRegistry);
    }
}