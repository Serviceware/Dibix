using System;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class UseCases
    {
        public static void T4_Fluent()
        {
            ITextTemplatingEngineHost host = null;
            IServiceProvider serviceProvider = null;
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromTextTemplate(host, serviceProvider)
                                                                                .Configure(a => a.AddSource("Dibix.Sdk.Tests.Database", b =>
                                                                                                 {
                                                                                                     b.SelectFolder("Tests/Parser")
                                                                                                      .SelectParser<SqlStoredProcedureParser>(c =>
                                                                                                      {
                                                                                                          c.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                                                                                                      });
                                                                                                 })
                                                                                                 .SelectOutputWriter<DaoWriter>(b =>
                                                                                                 {
                                                                                                     b.Formatting(CommandTextFormatting.Verbatim);
                                                                                                 })
                                                                                );
            ICodeGenerator generator = CodeGeneratorFactory.FromTextTemplate(configuration, host, serviceProvider);
            string generated = generator.Generate();
        }

        public static void T4_LoadJson()
        {
            ITextTemplatingEngineHost host = null;
            IServiceProvider serviceProvider = null;
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromTextTemplate(host, serviceProvider)
                                                                                .LoadJson(@"C:\");

        }

        public static void FileListener()
        {
            string executingFilePath = null;
            IServiceProvider serviceProvider = null;
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromVisualStudio(serviceProvider, executingFilePath)
                                                                                .ParseJson("{}");

            //configuration.Input.Sources.OfType<IPhysicalFileSource>();
        }


        public static void CustomTool()
        {
            IServiceProvider serviceProvider = null;
            string inputFilePath = null, @namespace = null;
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromVisualStudio(serviceProvider, inputFilePath)
                                                                    .ParseJson("{}");
            ICodeGenerator generator = CodeGeneratorFactory.FromCustomTool(configuration, serviceProvider, inputFilePath, @namespace);
            string generated = generator.Generate();
        }
    }
}