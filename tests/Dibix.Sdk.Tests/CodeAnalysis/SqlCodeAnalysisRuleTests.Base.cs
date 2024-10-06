using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Sql;
using Dibix.Testing;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests : TestBase
    {
        partial void Execute()
        {
            IEnumerable<SqlCodeAnalysisError> Analyze(ISqlCodeAnalysisRuleEngine engine)
            {
                // Determine rule by 'back in time development' and create instance
                Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{TestContext.TestName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
                SqlCodeAnalysisRuleAttribute descriptor = ruleType.GetCustomAttribute<SqlCodeAnalysisRuleAttribute>();
                string violationScriptPath = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{descriptor.Id:D3}.sql");
                return engine.Analyze(violationScriptPath, ruleType);
            }
            Execute(Analyze);
        }

        private void ExecuteScript(string scriptName)
        {
            IEnumerable<SqlCodeAnalysisError> Analyze(ISqlCodeAnalysisRuleEngine engine)
            {
                string scriptFilePath = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "Scripts", $"{scriptName}.sql");
                string normalizedScriptContent = SqlCmdParser.ProcessSqlCmdScript(scriptFilePath);
                return engine.AnalyzeScript(scriptFilePath, normalizedScriptContent);
            }
            Execute(Analyze);
        }

        private void Execute(Func<ISqlCodeAnalysisRuleEngine, IEnumerable<SqlCodeAnalysisError>> handler)
        {
            string resourceKey = ResourceUtility.BuildResourceKey($"CodeAnalysis.{TestContext.TestName}.xml");
            string expected = base.GetEmbeddedResourceContent(resourceKey);

            ICollection<TaskItem> source = CollectSource("Build");
            ICollection<TaskItem> scriptSource = CollectSource("PostDeploy");

            // master.dacpac is actually required otherwise causes this error:
            // CodeAnalysis\dbx_codeanalysis_error_002.sql(84,10,84,10):error 71502:Procedure: [dbo].[dbx_codeanalysis_error_002_x] has an unresolved reference to object [dbo].[sp_executesql].
            // The master.dacpac lies in the VS IDE folder with an installed SSDT, which is not available in CI builds, therefore ignore it for now.
            ICollection<TaskItem> references = Array.Empty<TaskItem>();

            TestLogger logger = new TestLogger(base.Out, distinctErrorLogging: true);

            Sdk.CodeAnalysis.SqlCodeAnalysisConfiguration configuration = new Sdk.CodeAnalysis.SqlCodeAnalysisConfiguration
            {
                IsEmbedded = false,
                LimitDdlStatements = true,
                NamingConventionPrefix = "dbx"
            };
            configuration.Source.AddRange(source);
            configuration.ScriptSource.AddRange(scriptSource);
            using PublicSqlDataSchemaModel publicSqlDataSchemaModel = PublicSqlDataSchemaModelLoader.Load(preventDmlReferences: true, DatabaseTestUtility.DatabaseSchemaProviderName, DatabaseTestUtility.ModelCollation, source, references, logger);
            using LockEntryManager lockEntryManager = LockEntryManager.Create(reset: false, filePath: null);
            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create(publicSqlDataSchemaModel.Model, configuration, lockEntryManager, logger);
            IEnumerable<SqlCodeAnalysisError> errors = handler(engine);

            string actual = GenerateXmlFromResults(errors);
            base.AssertEqual(expected, actual, extension: "xml");
        }

        private static ICollection<TaskItem> CollectSource(string elementName)
        {
            return DatabaseTestUtility.QueryProject($"x:Project/x:ItemGroup/x:{elementName}")
                                      .Select(x => x.Attribute("Include")!.Value)
                                      .Select(x => new TaskItem(x) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, x.Replace('\\', Path.DirectorySeparatorChar)) })
                                      .ToArray();
        }

        private static string GenerateXmlFromResults(IEnumerable<SqlCodeAnalysisError> result)
        {
            XDocument doc = new XDocument
            (
                new XElement
                (
                    "errors"
                  , result.Select
                    (
                        x => new XElement
                        (
                            "error"
                          , new XAttribute("ruleid", x.RuleId)
                          , new XAttribute("message", x.Message)
                          , new XAttribute("line", x.Line)
                          , new XAttribute("column", x.Column)
                        )
                    )
                    .ToArray()
                )
            );

            using (TextWriter stringWriter = new Utf8StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, NewLineOnAttributes = true }))
                {
                    doc.Save(xmlWriter);
                }
                return stringWriter.ToString();
            }
        }

        private sealed class Utf8StringWriter : StringWriter { public override Encoding Encoding => Encoding.UTF8; }
    }
}