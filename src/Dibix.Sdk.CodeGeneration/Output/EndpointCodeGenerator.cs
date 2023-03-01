﻿using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeGenerator : CodeGenerator
    {
        #region Constructor
        public EndpointCodeGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) : base(model, schemaRegistry, logger) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            const bool accessorOnly = false;
            const bool assumeEmbeddedActionTargets = true;
            const CodeGenerationOutputFilter outputFilter = CodeGenerationOutputFilter.Referenced;
            yield return new DaoExecutorWriter(model, outputFilter, accessorOnly);
            yield return new DaoExecutorInputClassWriter(model, outputFilter);
            yield return new DaoContractClassWriter(model, outputFilter);
            yield return new DaoStructuredTypeWriter(model, outputFilter);
            yield return new ApiDescriptionWriter(assumeEmbeddedActionTargets);
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model)
        {
            yield return new CSharpAnnotation("ArtifactAssembly");
        }

        protected override void OnContextCreated(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");
        }
        #endregion
    }
}