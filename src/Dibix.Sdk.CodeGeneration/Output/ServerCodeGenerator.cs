using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ServerCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ServerCodeGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) : base(model, schemaRegistry, logger) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            bool accessorOnly = model.EnableExperimentalFeatures;
            const CodeGenerationOutputFilter outputFilter = CodeGenerationOutputFilter.Local;
            yield return new DaoExecutorWriter(model, outputFilter, accessorOnly);
            yield return new DaoExecutorInputClassWriter(model, outputFilter);
            yield return new DaoContractClassWriter(model, outputFilter);
            yield return new DaoStructuredTypeWriter(model, outputFilter);
            
            if (!model.EnableExperimentalFeatures)
                yield return new ApiDescriptionWriter(assumeEmbeddedActionTargets: false);
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