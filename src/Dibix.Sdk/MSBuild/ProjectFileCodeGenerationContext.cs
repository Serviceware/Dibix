using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class ProjectFileCodeGenerationContext : ICodeGenerationContext
    {
        public GeneratorConfiguration Configuration { get; }
        public string RootNamespace { get; }
        public string DefaultClassName { get; }
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public ProjectFileCodeGenerationContext(GeneratorConfiguration configuration, string @namespace, string configurationName, IAssemblyLocator assemblyLocator, IErrorReporter errorReporter)
        {
            this.Configuration = configuration;
            this.RootNamespace = @namespace;
            this.DefaultClassName = configurationName;
            this.ContractResolverFacade = new ContractResolverFacade(assemblyLocator);
            this.ErrorReporter = errorReporter;
        }

        public void CollectAdditionalArtifacts(SourceArtifacts artifacts) { }
    }
}