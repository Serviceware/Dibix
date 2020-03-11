﻿using Dibix.Sdk.MSBuild;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal interface ICodeArtifactsGenerator
    {
        bool Generate(CodeArtifactsGenerationModel model, ISchemaRegistry schemaRegistry, IErrorReporter errorReporter);
    }
}
