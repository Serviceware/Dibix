using System.IO;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class CustomToolCodeGenerationContext : ICodeGenerationContext
    {
        #region Properties
        public GeneratorConfiguration Configuration { get; }
        public string RootNamespace { get; }
        public string DefaultClassName { get; }
        public IContractResolverFacade ContractResolver { get; }
        public IErrorReporter ErrorReporter { get; }
        #endregion

        #region Constructor
        public CustomToolCodeGenerationContext(GeneratorConfiguration configuration, IContractResolverFacade contractResolver, IErrorReporter errorReporter, string inputFilePath, string @namespace)
        {
            this.Configuration = configuration;
            this.ErrorReporter = errorReporter;
            this.RootNamespace = @namespace;
            this.ContractResolver = contractResolver;
            this.DefaultClassName = Path.GetFileNameWithoutExtension(inputFilePath);
        }
        #endregion

        #region Public Methods
        public void CollectAdditionalArtifacts(SourceArtifacts artifacts) { }
        #endregion
    }
}