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
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromTextTemplate(GeneratorConfiguration configuration, ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            IContractResolver contractResolver = new CodeElementContractResolver(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IContractResolverFacade contractResolverFacade = new ContractResolverFacade(assemblyLocator);
            contractResolverFacade.RegisterContractResolver(contractResolver, 0);
            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            ICodeGenerationContext context = new TextTemplateCodeGenerationContext(configuration, contractResolverFacade, errorReporter, textTemplatingEngineHost, serviceProvider);
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }

        public static ICodeGenerator FromCustomTool(GeneratorConfiguration configuration, IServiceProvider serviceProvider, string inputFilePath, string @namespace)
        {
            IContractResolver contractResolver = new CodeElementContractResolver(serviceProvider, inputFilePath);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, inputFilePath);
            IContractResolverFacade contractResolverFacade = new ContractResolverFacade(assemblyLocator);
            contractResolverFacade.RegisterContractResolver(contractResolver, 0);
            if (_errorReporter == null)
                _errorReporter = new VisualStudioErrorReporter(serviceProvider);

            ICodeGenerationContext context = new CustomToolCodeGenerationContext(configuration, contractResolverFacade, _errorReporter, inputFilePath, @namespace);
            ICodeGenerator generator = new DaoCodeGenerator(context);
            return generator;
        }
    }
}