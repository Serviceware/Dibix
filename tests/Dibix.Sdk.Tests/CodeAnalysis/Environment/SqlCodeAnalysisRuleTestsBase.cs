using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Tests.Utilities;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;

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

            // Load script that violates exactly this rule
            TSqlModel model = new TSqlModel(SqlServerVersion.Sql130, new TSqlModelOptions());
            model.AddObjects(File.ReadAllText($@"..\..\..\..\Dibix.Sdk.Tests.Database\CodeAnalysis\dbx_codeanalysis_error_{ruleInstance.Id:D3}.sql"));

            // Create DacFX code analysis engine
            CodeAnalysisService service = new CodeAnalysisServiceFactory().CreateAnalysisService(model.Version);

            // Here we have to forcefully make DacFX compose our rule
            CompositionProperties properties = (CompositionProperties)service.GetType().GetField("_properties", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(service);
            properties.AssemblyLookupPath = Environment.CurrentDirectory; // Make DacFX compose the test assembly to scan for rules

            // Load rules
            foreach (RuleDescriptor ruleDescriptor in service.GetRules())
            {
                if (ruleDescriptor.RuleId == SqlCodeAnalysisRuleDecorator.RuleId)
                {
                    // Apply current rule instance to composable rule decorator
                    SqlCodeAnalysisRuleDecorator decorator = (SqlCodeAnalysisRuleDecorator)ruleDescriptor.Rule;
                    decorator.Rule = ruleInstance;
                }
                else
                {
                    // Disable all built in rules
                    ruleDescriptor.Enabled = false;
                }
            }

            CodeAnalysisResult result = service.Analyze(model);

            string actual = GenerateXmlFromResults(result);
            TestUtilities.AssertEqualWithDiffTool(expected, actual);
        }

        private static string GetExpectedText(string key)
        {
            string resource = Resource.ResourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }

        private static string GenerateXmlFromResults(CodeAnalysisResult result)
        {
            XDocument doc = new XDocument(new XElement("errors", result.Problems.Select(x => new XElement("error",
                new XAttribute("description", x.Description),
                new XAttribute("line", x.StartLine),
                new XAttribute("column", x.StartColumn))).ToArray()));

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