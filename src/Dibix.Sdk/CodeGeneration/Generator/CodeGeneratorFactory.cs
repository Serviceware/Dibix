using System;
using System.IO;
using Dibix.Sdk.VisualStudio;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGeneratorFactory
    {
        private static IErrorReporter _errorReporter;

        public static ICodeGenerator Create(ICodeGenerationContext context)
        {
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromTextTemplate(GeneratorConfiguration configuration, ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            ITypeLoader typeLoader = new CodeElementTypeLoader(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IFileSystemProvider fileSystemProvider = new ProjectFileSystemProvider(serviceProvider, textTemplatingEngineHost.TemplateFile);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider, assemblyLocator);
            typeLoaderFacade.RegisterTypeLoader(typeLoader);
            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            ICodeGenerationContext context = new TextTemplateCodeGenerationContext(configuration, typeLoaderFacade, errorReporter, textTemplatingEngineHost, serviceProvider);
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromCustomTool(GeneratorConfiguration configuration, IServiceProvider serviceProvider, string inputFilePath, string @namespace)
        {
            ITypeLoader typeLoader = new CodeElementTypeLoader(serviceProvider, inputFilePath);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, inputFilePath);
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(Path.GetDirectoryName(inputFilePath));
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider, assemblyLocator);
            typeLoaderFacade.RegisterTypeLoader(typeLoader);
            if (_errorReporter == null)
                _errorReporter = new VisualStudioErrorReporter(serviceProvider);

            ICodeGenerationContext context = new CustomToolCodeGenerationContext(configuration, typeLoaderFacade, _errorReporter, inputFilePath, @namespace);
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }
    }
}