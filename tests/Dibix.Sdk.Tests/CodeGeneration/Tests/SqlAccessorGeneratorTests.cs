using Dibix.Sdk.CodeGeneration;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed class SqlAccessorGeneratorTests : SqlAccessorGeneratorTestBase
    {
        [Fact]
        public void ParserTest()
        {
            base.RunGeneratorTest(cfg =>
            {
                cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                    {
                        x.SelectFolder("Tests/Parser")
                         .SelectParser<SqlStoredProcedureParser>(y =>
                         {
                             y.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                         });
                    })
                    .SelectOutputWriter<SqlDaoWriter>(x =>
                    {
                        x.Formatting(SqlQueryOutputFormatting.Verbatim);
                    });
            });
        }

        [Fact]
        public void SourcesTest()
        {
            base.RunGeneratorTest(cfg =>
            {
                cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                    {
                        x.SelectFolder(null, "CodeAnalysis", "Tables", "Types", "Tests/Parser", "Tests/Sources/Excluded", "Tests/Sources/dbx_tests_sources_externalsp")
                         .SelectFile("Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql")
                         .SelectParser<SqlStoredProcedureParser>(y =>
                         {
                             y.Formatter<TakeSourceSqlStatementFormatter>();
                         });
                    })
                    .AddSource("Dibix.Sdk.Tests.Database", x =>
                    {
                        x.SelectFile("Tests/Sources/dbx_tests_sources_externalsp.sql")
                         .SelectParser<SqlStoredProcedureParser>(y =>
                         {
                             y.Formatter<ExecStoredProcedureSqlStatementFormatter>();
                         });
                    })
                    .SelectOutputWriter<SqlDaoWriter>(x =>
                    {
                        x.Namespace("This.Is.A.Custom.Namespace")
                         .ClassName("Accessor")
                         .Formatting(SqlQueryOutputFormatting.Verbatim);
                    });
            });
        }
    }
}