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
        public void FluentSourcesTest()
        {
            base.RunGeneratorTest("SourcesTest", cfg =>
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
                    .AddDacPac("SSISDB.dacpac", x =>
                    {
                        x.SelectProcedure("[catalog].[delete_project]", "DeleteProject")
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

        [Fact]
        public void JsonSourcesTest()
        {
            base.RunGeneratorTest("SourcesTest", @"{
    ""input"": {
        ""Dibix.Sdk.Tests.Database"": {
		    ""include"": [
				""./**""
			  , ""Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql""
			],
            ""exclude"": [
                ""CodeAnalysis""
			  , ""Tables""
			  , ""Types""
			  , ""Tests/Parser""
			  , ""Tests/Sources/Excluded""
			  , ""Tests/Sources/dbx_tests_sources_externalsp""
            ],
			""parser"": ""SqlStoredProcedureParser"",
			""formatter"": ""TakeSourceSqlStatementFormatter""
        },
        ""SSISDB.dacpac"": {
            ""include"": {
                ""[catalog].[delete_project]"": ""DeleteProject""
            },
            ""parser"": ""SqlStoredProcedureParser"",
            ""formatter"": ""ExecStoredProcedureSqlStatementFormatter""
        }
    },
    ""output"": {
        ""name"": ""SqlDaoWriter"",
        ""namespace"": ""This.Is.A.Custom.Namespace"",
        ""className"": ""Accessor"",
        ""formatting"": ""Verbatim""
    }
}");
        }
    }
}