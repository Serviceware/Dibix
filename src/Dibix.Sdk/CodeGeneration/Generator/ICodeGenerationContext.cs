namespace Dibix.Sdk.CodeGeneration
{
    public interface ICodeGenerationContext
    {
        GeneratorConfiguration Configuration { get; }
        string Namespace { get; }
        string ClassName { get; }
        ITypeLoaderFacade TypeLoaderFacade { get; }
        IErrorReporter ErrorReporter { get; }
    }
}