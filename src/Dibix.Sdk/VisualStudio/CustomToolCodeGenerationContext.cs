using System.IO;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class CustomToolCodeGenerationContext : ICodeGenerationContext
    {
        #region Properties
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }
        #endregion

        #region Constructor
        public CustomToolCodeGenerationContext(GeneratorConfiguration configuration, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter, string inputFilePath, string @namespace)
        {
            this.Configuration = configuration;
            this.ErrorReporter = errorReporter;
            this.Namespace = @namespace;
            this.ContractResolverFacade = contractResolverFacade;
            this.ClassName = Path.GetFileNameWithoutExtension(inputFilePath);
        }
        #endregion

        #region Public Methods
        public void CollectAdditionalArtifacts(SourceArtifacts artifacts) { }
        #endregion
    }
}