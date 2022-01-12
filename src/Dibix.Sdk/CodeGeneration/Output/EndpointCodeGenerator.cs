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
            const SchemaDefinitionSource schemaFilter = SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated | SchemaDefinitionSource.Foreign;
            yield return new DaoExecutorWriter(accessorOnly);
            yield return new DaoExecutorInputClassWriter();
            yield return new DaoContractClassWriter(model);
            yield return new DaoStructuredTypeWriter(model, schemaFilter);
            yield return new ApiDescriptionWriter();
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model, bool isArtifactAssembly)
        {
            if (isArtifactAssembly)
                yield return new CSharpAnnotation("ArtifactAssembly");
        }
        #endregion
    }
}