using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Tests.Utilities;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRuleTestsBase
    {
        protected void Execute()
        {
            // Determine rule by 'back in time development' and create instance
            string ruleName = new StackTrace().GetFrame(1).GetMethod().Name;
            string expected = GetExpectedText(ruleName);
            Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{ruleName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
            ISqlCodeAnalysisRule ruleInstance = (ISqlCodeAnalysisRule)Activator.CreateInstance(ruleType);
            string violationScriptPath = $@"..\..\..\..\Dibix.Sdk.Tests.Database\CodeAnalysis\dbx_codeanalysis_error_{ruleInstance.Id:D3}.sql";

            TestUtilities.OverrideNamingConventions();
            ISqlCodeAnalysisRuleEngine engine = new SqlCodeAnalysisRuleEngine();
            IEnumerable<SqlCodeAnalysisError> errors = engine.Analyze(ruleInstance, violationScriptPath);

            string actual = GenerateXmlFromResults(errors);
            TestUtilities.AssertEqualWithDiffTool(expected, actual);
        }

        private static string GetExpectedText(string key)
        {
            ResourceManager resourceManager = new ResourceManager("Dibix.Sdk.Tests.Resource", typeof(SqlCodeAnalysisRuleTestsBase).Assembly);
            string resource = resourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }

        private static string GenerateXmlFromResults(IEnumerable<SqlCodeAnalysisError> result)
        {
            XDocument doc = new XDocument(new XElement("errors", result.Select(x => new XElement("error",
                new XAttribute("message", x.Message),
                new XAttribute("line", x.Line),
                new XAttribute("column", x.Column))).ToArray()));

            StringBuilder sb = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(sb))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, NewLineOnAttributes = true }))
                {
                    doc.Save(xmlWriter);
                }
            }

            return sb.ToString();
        }
    }
}