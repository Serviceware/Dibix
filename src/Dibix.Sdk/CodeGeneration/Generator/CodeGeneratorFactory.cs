using System;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGeneratorFactory
    {
        public static ICodeGenerator Create(ICodeGenerationContext context)
        {
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromTextTemplate(GeneratorConfiguration configuration, ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            ITypeLoader typeLoader = new VisualStudioTypeLoader(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IAssemblyLocator assemblyLocator = new VisualStudioAssemblyLocator(serviceProvider, textTemplatingEngineHost.TemplateFile);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(typeLoader, assemblyLocator);
            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            ICodeGenerationContext context = new TextTemplateCodeGenerationContext(configuration, typeLoaderFacade, errorReporter, textTemplatingEngineHost, serviceProvider);
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromCustomTool(GeneratorConfiguration configuration, IServiceProvider serviceProvider, string inputFilePath, string @namespace)
        {
            ITypeLoader typeLoader = new VisualStudioTypeLoader(serviceProvider, inputFilePath);
            IAssemblyLocator assemblyLocator = new VisualStudioAssemblyLocator(serviceProvider, inputFilePath);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(typeLoader, assemblyLocator);
            IErrorReporter errorReporter = new VisualStudioErrorReporter(serviceProvider);
            ICodeGenerationContext context = new CustomToolCodeGenerationContext(configuration, typeLoaderFacade, errorReporter, inputFilePath, @namespace);
            ICodeGenerator generator = new CodeGenerator(context);
            return generator;
        }
    }
}