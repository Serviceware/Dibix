using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientCodeGenerator : CodeGenerator
    {
        #region Constructor
        public ClientCodeGenerator(ILogger logger, ISchemaRegistry schemaRegistry) : base(logger, schemaRegistry) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters()
        {
            yield return new ClientContractClassWriter();
            yield return new ApiClientInterfaceWriter();
            yield return new ApiClientImplementationWriter();
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(bool isArtifactAssembly)
        {
            if (isArtifactAssembly)
                yield return new CSharpAnnotation("ArtifactAssembly");
        }

        protected override void OnContextCreated(CodeGenerationContext context, bool isArtifactAssembly)
        {
            if (isArtifactAssembly)
                context.AddUsing("Dibix.Http.Client");
        }
        #endregion
    }
}