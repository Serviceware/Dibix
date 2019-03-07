using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Data.Tools.Schema.Extensibility;

namespace Dibix.Sdk
{
    public static class VisualStudioExtensionAdapter
    {
        private static IErrorReporter _errorReporter;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            RegisterSqlCodeAnalysisRule();
            _errorReporter = new VisualStudioErrorReporter(serviceProvider);
            VisualStudioCodeGenerationWatcher.Initialize(serviceProvider, _errorReporter);
        }

        public static string InvokeGenerator(string inputFilePath, string inputFileContents, string @namespace, IServiceProvider serviceProvider)
        {
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromVisualStudio(serviceProvider, inputFilePath, _errorReporter)
                                                                                .ParseJson(inputFileContents);
            ICodeGenerator generator = CodeGeneratorFactory.FromCustomTool(configuration, serviceProvider, inputFilePath, @namespace);
            string generated = generator.Generate();
            return generated;
        }

        private static void RegisterSqlCodeAnalysisRule()
        {
            // This is a hack to add our SQL code analysis rule without planting a separate DLL in the DAC extensions directory
            Assembly extensionManagerAssembly = typeof(IExtension).Assembly;
            Type extensionManagerType = extensionManagerAssembly.GetType("Microsoft.SqlServer.Dac.Extensibility.ExtensionManagerFactory", true);
            PropertyInfo defaultCatalogProperty = extensionManagerType.GetProperty("DefaultCatalog", BindingFlags.NonPublic | BindingFlags.Static);
            AggregateCatalog catalog = (AggregateCatalog)defaultCatalogProperty.GetValue(null);
            catalog.Catalogs.Add(new TypeCatalog(typeof(AggregateSqlCodeAnalysisRule)));
        }
    }
}
