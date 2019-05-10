using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class ProjectFileCodeGenerationContext : ICodeGenerationContext
    {
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public ProjectFileCodeGenerationContext(GeneratorConfiguration configuration, string @namespace, string configurationName, IFileSystemProvider fileSystemProvider, IAssemblyLocator assemblyLocator, IErrorReporter errorReporter)
        {
            this.Configuration = configuration;
            this.Namespace = @namespace;
            this.ClassName = configurationName;
            this.ContractResolverFacade = new ContractResolverFacade(fileSystemProvider, assemblyLocator);
            this.ErrorReporter = errorReporter;
        }
    }
}