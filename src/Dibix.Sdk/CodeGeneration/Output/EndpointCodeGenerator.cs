using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeGenerator : CodeGenerator
    {
        #region Constructor
        public EndpointCodeGenerator(CodeGenerationModel model, ILogger logger, ISchemaRegistry schemaRegistry) : base(model, logger, schemaRegistry) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            const bool accessorOnly = false;
            const bool assumeEmbeddedActionTargets = true;
            const SchemaDefinitionSource schemaFilter = SchemaDefinitionSource.Local | SchemaDefinitionSource.Foreign;
            yield return new DaoExecutorWriter(model, schemaFilter, accessorOnly);
            yield return new DaoExecutorInputClassWriter(model, schemaFilter);
            yield return new DaoContractClassWriter(model, schemaFilter);
            yield return new DaoStructuredTypeWriter(model, schemaFilter);
            yield return new ApiDescriptionWriter(assumeEmbeddedActionTargets);
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model)
        {
            yield return new CSharpAnnotation("ArtifactAssembly");
        }
        #endregion
    }
}