using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ClientCodeGenerator(CodeGenerationModel model, ILogger logger, ISchemaRegistry schemaRegistry) : base(model, logger, schemaRegistry) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            yield return new ClientContractClassWriter(model);
            yield return new ApiClientInterfaceWriter();
            yield return new ApiClientImplementationWriter();
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model)
        {
            yield return new CSharpAnnotation("ArtifactAssembly");
        }

        protected override void OnContextCreated(CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Client");
        }
        #endregion
    }
}