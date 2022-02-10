using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests
    {
        private readonly ITestOutputHelper _output;

        public SqlCodeAnalysisRuleTests(ITestOutputHelper output) => this._output = output;

        private void Execute()
        {
            // Determine rule by 'back in time development' and create instance
            string ruleName = new StackTrace().GetFrame(1).GetMethod().Name;
            string expected = TestUtility.GetExpectedText(ruleName);
            Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{ruleName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
            ISqlCodeAnalysisRule ruleInstance = (ISqlCodeAnalysisRule)Activator.CreateInstance(ruleType);
            SqlCodeAnalysisRuleAttribute descriptor = ruleType.GetCustomAttribute<SqlCodeAnalysisRuleAttribute>();
            string violationScriptPath = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{descriptor.Id:D3}.sql");

            IEnumerable<TaskItem> sources = ((IEnumerable)DatabaseTestUtility.QueryProject("x:Project/x:ItemGroup/x:Build/@Include"))
                                                                             .OfType<XAttribute>()
                                                                             .Select(x => new TaskItem(x.Value) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, x.Value) })
                                                                             .ToArray();

            TestLogger logger = new TestLogger(this._output);

            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(DatabaseTestUtility.ProjectName, DatabaseTestUtility.DatabaseSchemaProviderName, DatabaseTestUtility.ModelCollation, sources, Array.Empty<TaskItem>(), logger);
            LockEntryManager lockEntryManager = LockEntryManager.Create();
            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create(model, DatabaseTestUtility.ProjectName, new SqlCodeAnalysisConfiguration("dbx"), false, lockEntryManager, logger);
            IEnumerable<SqlCodeAnalysisError> errors = engine.Analyze(violationScriptPath, ruleInstance);

            string actual = GenerateXmlFromResults(errors);
            TestUtility.AssertEqualWithDiffTool(expected, actual, "xml");
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