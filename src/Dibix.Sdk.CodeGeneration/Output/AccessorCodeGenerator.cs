﻿using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class AccessorCodeGenerator : CodeGenerator
    {
        #region Constructor
        public AccessorCodeGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) : base(model, schemaRegistry, logger) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            const CodeGenerationOutputFilter outputFilter = CodeGenerationOutputFilter.Local;
            const ActionCompatibilityLevel compatibilityLevel = ActionCompatibilityLevel.Reflection;
            yield return new DaoExecutorWriter(model, outputFilter);
            yield return new DaoExecutorInputClassWriter(model, outputFilter);
            yield return new DaoContractClassWriter(model, outputFilter, compatibilityLevel, JsonSerializerFlavor.NewtonsoftJson);
            yield return new DaoStructuredTypeWriter(model, outputFilter);
            yield return new ApiDescriptionWriter(model, compatibilityLevel);
            yield return new ApiControllerClassWriter(model);
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