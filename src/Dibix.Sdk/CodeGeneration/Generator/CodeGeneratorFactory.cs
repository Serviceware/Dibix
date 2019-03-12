using System;
using Dibix.Sdk.VisualStudio;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGeneratorFactory
    {
        private static IErrorReporter _errorReporter;

        public static ICodeGenerator Create(ICodeGenerationContext context)
        {
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromTextTemplate(GeneratorConfiguration configuration, ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            ITypeLoader typeLoader = new CodeElementTypeLoader(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, textTemplatingEngineHost.TemplateFile);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(typeLoader, assemblyLocator);
            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            ICodeGenerationContext context = new TextTemplateCodeGenerationContext(configuration, typeLoaderFacade, errorReporter, textTemplatingEngineHost, serviceProvider);
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromCustomTool(GeneratorConfiguration configuration, IServiceProvider serviceProvider, string inputFilePath, string @namespace)
        {
            ITypeLoader typeLoader = new CodeElementTypeLoader(serviceProvider, inputFilePath);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, inputFilePath);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(typeLoader, assemblyLocator);
            if (_errorReporter == null)
                _errorReporter = new VisualStudioErrorReporter(serviceProvider);

            ICodeGenerationContext context = new CustomToolCodeGenerationContext(configuration, typeLoaderFacade, _errorReporter, inputFilePath, @namespace);
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }
    }
}