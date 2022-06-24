using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ServerCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ServerCodeGenerator(CodeGenerationModel model, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger) : base(model, schemaDefinitionResolver, logger) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            bool accessorOnly = model.EnableExperimentalFeatures;
            const SchemaDefinitionSource schemaFilter = SchemaDefinitionSource.Local;
            yield return new DaoExecutorWriter(model, schemaFilter, accessorOnly);
            yield return new DaoExecutorInputClassWriter(model, schemaFilter);
            yield return new DaoContractClassWriter(model, schemaFilter);
            yield return new DaoStructuredTypeWriter(model, schemaFilter);
            
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