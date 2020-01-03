namespace Dibix.Sdk.CodeGeneration
{
    public interface ICodeGenerationContext
    {
        GeneratorConfiguration Configuration { get; }
        string RootNamespace { get; }
        string DefaultClassName { get; }
        IContractResolverFacade ContractResolver { get; }
        IErrorReporter ErrorReporter { get; }

        void CollectAdditionalArtifacts(SourceArtifacts artifacts);
    }
}