using System.Text.RegularExpressions;
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
                                                     .SelectOutputWriter<ArtifactWriterBase>(x => { x.Formatting(CommandTextFormatting.MultiLine); }));

            Assert(generated);
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
                                                     .SelectOutputWriter<ArtifactWriterBase>(x =>
                                                     {
                                                         x.Namespace("This.Is.A.Custom.Namespace")
                                                          .ClassName("Accessor")
                                                          .Formatting(CommandTextFormatting.MultiLine);
                                                     }));

            Assert("SourcesTest", generated);
        }

        private static void Assert(string generated) => Assert(TestUtility.TestName, generated);
        private static void Assert(string expectedTextKey, string generated)
        {
            generated = Regex.Replace(generated
                                    , @"\[GeneratedCodeAttribute\(""Dibix\.Sdk"", ""[\d]+\.[\d]+\.[\d]+\.[\d]+""\)\]"
                                    , "[GeneratedCodeAttribute(\"Dibix.Sdk\", \"1.0.0.0\")]");
            string expectedText = TestUtility.GetExpectedText(expectedTextKey);
            string actualText = generated;
            TestUtility.AssertEqualWithDiffTool(expectedText, actualText, "cs");
        }
    }
}