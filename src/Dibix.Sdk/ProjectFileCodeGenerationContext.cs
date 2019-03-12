using System;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk
{
    internal sealed class ProjectFileCodeGenerationContext : ICodeGenerationContext
    {
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public ITypeLoaderFacade TypeLoaderFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public ProjectFileCodeGenerationContext(GeneratorConfiguration configuration, string @namespace, string configurationName, IAssemblyLocator assemblyLocator, IErrorReporter errorReporter)
        {
            this.Configuration = configuration;
            this.Namespace = @namespace;
            this.ClassName = configurationName;
            this.TypeLoaderFacade = new TypeLoaderFacade(new TypeLoader(), assemblyLocator);
            this.ErrorReporter = errorReporter;
        }

        private class TypeLoader : ITypeLoader
        {
            public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
            {
                throw new NotImplementedException();
            }
        }
    }
}