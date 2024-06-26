﻿using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class AccessorCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<AccessorCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.AccessorTargetFileName);
        protected override string GetOutputName(CodeGenerationModel model) => model.AccessorTargetFileName;
        protected override AccessorCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new AccessorCodeGenerator(model, schemaRegistry, logger);
    }
}