using Dibix.Sdk.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.VisualStudio.Tests
{
    [TestClass]
    public sealed partial class TextTemplateCodeGeneratorTests
    {
        [TestMethod]
        public void ParserTest()
        {
            string generated = this.ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                                          {
                                                              x.SelectFolder("Tests/Parser")
                                                               .SelectParser<SqlStoredProcedureParser>(y =>
                                                               {
                                                                   y.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                                                               });
                                                          })
                                                          .SelectOutputWriter<ArtifactWriterBase>(x => { x.Formatting(CommandTextFormatting.MultiLine); }));
                                        
            this.Assert(generated);
        }

        [TestMethod]
        public void FluentSourcesTest()
        {
            string generated = this.ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                                          {
                                                              x.SelectFolder(null, "CodeAnalysis", "Tables", "Types", "Tests/Parser", "Tests/Sources/Excluded", "Tests/Sources/dbx_tests_sources_externalsp", "Tests/Syntax")
                                                               .SelectFile("Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql");
                                                          })
                                                          .AddSource("Dibix.Sdk.Tests.Database", x =>
                                                          {
                                                              x.SelectFile("Tests/Sources/dbx_tests_sources_externalsp.sql")
                                                               .SelectParser<SqlStoredProcedureParser>(y => { y.Formatter<ExecStoredProcedureSqlStatementFormatter>(); });
                                                          })
                                                          .AddDacPac("SSISDB.dacpac", x =>
                                                          {
                                                              x.SelectProcedure("[catalog].[delete_project]", "DeleteProject")
                                                               .SelectParser<SqlStoredProcedureParser>(y => { y.Formatter<ExecStoredProcedureSqlStatementFormatter>(); });
                                                          })
                                                          .SelectOutputWriter<ArtifactWriterBase>(x =>
                                                          {
                                                              x.Namespace("This.Is.A.Custom.Namespace")
                                                               .ClassName("Accessor")
                                                               .Formatting(CommandTextFormatting.MultiLine);
                                                          }));

            this.Assert("SourcesTest", generated);
        }
    }
}