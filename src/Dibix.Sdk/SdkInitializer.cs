using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Data.Tools.Schema.Extensibility;

namespace Dibix.Sdk
{
    public static class SdkInitializer
    {
        public static void Initialize()
        {
            RegisterSqlCodeAnalysisRule();
        }

        public static string InvokeGenerator(string sourceFilePath, string sourceFileContents, string @namespace, ReportError errorReporter, IServiceProvider serviceProvider)
        {
            string generated = SqlAccessorGeneratorFactory.FromVisualStudio(new CodeGeneratorContext(sourceFilePath, @namespace, errorReporter), serviceProvider)
                                                          .ParseJson(sourceFileContents);
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
