using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CustomToolCodeGenerationContext : ICodeGenerationContext
    {
        #region Properties
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public ITypeLoaderFacade TypeLoaderFacade { get; }
        public IErrorReporter ErrorReporter { get; }
        #endregion

        #region Constructor
        public CustomToolCodeGenerationContext(GeneratorConfiguration configuration, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter, string inputFilePath, string @namespace)
        {
            this.Configuration = configuration;
            this.ErrorReporter = errorReporter;
            this.Namespace = @namespace;
            this.TypeLoaderFacade = typeLoaderFacade;
            this.ClassName = Path.GetFileNameWithoutExtension(inputFilePath);
        }
        #endregion
    }
}