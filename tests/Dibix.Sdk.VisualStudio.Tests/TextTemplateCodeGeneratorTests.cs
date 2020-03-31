using Dibix.Sdk.CodeGeneration;
using Dibix.Tests;
using Xunit;

namespace Dibix.Sdk.VisualStudio.Tests
{
    public sealed partial class TextTemplateCodeGeneratorTests
    {
        [Fact]
        public void ParserTest()
        {
            string generated = ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                                     {
                                                         x.SelectFolder("Tests/Parser")
                                                          .SelectParser<SqlStoredProcedureParser>(y =>
                                                          {
                                                              y.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                                                          });
                                                     })
                                                     .SelectOutputWriter<DaoWriter>(x => { x.Formatting(CommandTextFormatting.Verbatim); }));

            TestUtility.Evaluate(generated);
        }

        [Fact]
        public void FluentSourcesTest()
        {
            string generated = ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
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
                                                     .SelectOutputWriter<DaoWriter>(x =>
                                                     {
                                                         x.Namespace("This.Is.A.Custom.Namespace")
                                                          .ClassName("Accessor")
                                                          .Formatting(CommandTextFormatting.Verbatim);
                                                     }));

            TestUtility.Evaluate("SourcesTest", generated);
        }
    }
}