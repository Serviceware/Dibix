using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ServerCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ServerCodeGenerator(CodeGenerationModel model, ILogger logger, ISchemaRegistry schemaRegistry) : base(model, logger, schemaRegistry) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            bool accessorOnly = model.EnableExperimentalFeatures;
            const SchemaDefinitionSource schemaFilter = SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated;
            yield return new DaoExecutorWriter(model, schemaFilter, accessorOnly);
            yield return new DaoExecutorInputClassWriter(model, schemaFilter);
            yield return new DaoContractClassWriter(model);
            yield return new DaoStructuredTypeWriter(model, schemaFilter);
            
            if (!model.EnableExperimentalFeatures)
                yield return new ApiDescriptionWriter();
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model, bool isArtifactAssembly)
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