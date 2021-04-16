﻿using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ServerCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ServerCodeGenerator(ILogger logger, ISchemaRegistry schemaRegistry) : base(logger, schemaRegistry) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters()
        {
            yield return new DaoExecutorWriter();
            yield return new DaoExecutorInputClassWriter();
            yield return new DaoContractClassWriter();
            yield return new DaoStructuredTypeWriter();
            yield return new ApiDescriptionWriter();
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(bool isArtifactAssembly)
        {
            if (isArtifactAssembly)
                yield return new CSharpAnnotation("ArtifactAssembly");
        }

        protected override void OnContextCreated(CodeGenerationContext context, bool isArtifactAssembly)
        {
            if (isArtifactAssembly)
                context.AddUsing("Dibix");
        }
        #endregion
    }
}