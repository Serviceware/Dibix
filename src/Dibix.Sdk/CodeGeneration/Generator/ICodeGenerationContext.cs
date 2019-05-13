namespace Dibix.Sdk.CodeGeneration
{
    public interface ICodeGenerationContext
    {
        GeneratorConfiguration Configuration { get; }
        string Namespace { get; }
        string ClassName { get; }
        IContractResolverFacade ContractResolverFacade { get; }
        IErrorReporter ErrorReporter { get; }

        void CollectAdditionalArtifacts(SourceArtifacts artifacts);
    }
}