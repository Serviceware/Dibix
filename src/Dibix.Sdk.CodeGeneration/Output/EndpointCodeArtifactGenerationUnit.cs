using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeArtifactGenerationUnit : CodeArtifactGenerationUnit<EndpointCodeGenerator>
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.EndpointTargetFileName);
        protected override string GetOutputName(CodeGenerationModel model) => model.EndpointTargetFileName;
        protected override EndpointCodeGenerator CreateGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) => new EndpointCodeGenerator(model, schemaRegistry, logger);
    }
}