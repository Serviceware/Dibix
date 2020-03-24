using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.CodeAnalysis;
using Moq;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests : DatabaseTestBase
    {
        private readonly ITestOutputHelper _output;

        public SqlCodeAnalysisRuleTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        private void Execute()
        {
            // Determine rule by 'back in time development' and create instance
            string ruleName = new StackTrace().GetFrame(1).GetMethod().Name;
            string expected = GetExpectedText(ruleName);
            Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{ruleName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
            ISqlCodeAnalysisRule ruleInstance = (ISqlCodeAnalysisRule)Activator.CreateInstance(ruleType);
            string violationScriptPath = Path.Combine(DatabaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{ruleInstance.Id:D3}.sql");

            IEnumerable<string> sources = ((IEnumerable)base.QueryProject("x:Project/x:ItemGroup/x:Build/@Include"))
                                                            .OfType<XAttribute>()
                                                            .Select(x => Path.Combine(DatabaseProjectDirectory, x.Value));

            StringBuilder errorOutput = new StringBuilder();

            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogMessage(It.IsAny<string>())).Callback<string>(this._output.WriteLine);
            logger.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                  .Callback((string code, string text, string source, int line, int column) => errorOutput.AppendLine(CanonicalLogFormat.ToErrorString(code, text, source, line, column)));
            logger.SetupGet(x => x.HasLoggedErrors).Returns(errorOutput.Length > 0);

            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create("dbx", base.DatabaseSchemaProviderName, base.ModelCollation, sources, Enumerable.Empty<string>(), logger.Object);
            IEnumerable<SqlCodeAnalysisError> errors = engine.Analyze(violationScriptPath, ruleInstance);

            string actual = GenerateXmlFromResults(errors);
            TestUtility.AssertEqualWithDiffTool(expected, actual, "xml");
        }

        private static string GetExpectedText(string key)
        {
            ResourceManager resourceManager = new ResourceManager("Dibix.Sdk.Tests.Resource", typeof(SqlCodeAnalysisRuleTests).Assembly);
            string resource = resourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
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