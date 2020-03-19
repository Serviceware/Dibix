using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Framework;
using Moq;
using Xunit;
using StringWriter = System.IO.StringWriter;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests : DatabaseTestBase
    {
        private void Execute()
        {
            // Determine rule by 'back in time development' and create instance
            string ruleName = new StackTrace().GetFrame(1).GetMethod().Name;
            string expected = GetExpectedText(ruleName);
            Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{ruleName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
            ISqlCodeAnalysisRule ruleInstance = (ISqlCodeAnalysisRule)Activator.CreateInstance(ruleType);
            string violationScriptPath = Path.Combine(DatabaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{ruleInstance.Id:D3}.sql");

            ITaskItem[] source = ((IEnumerable)base.QueryProject("x:Project/x:ItemGroup/x:Build/@Include"))
                                                   .OfType<XAttribute>()
                                                   .Select(x =>
                                                   {
                                                       Mock<ITaskItem> item = new Mock<ITaskItem>(MockBehavior.Strict);
                                                       item.SetupGet(y => y.MetadataNames).Returns(new string[0]);
                                                       item.Setup(y => y.GetMetadata("FullPath")).Returns(Path.Combine(DatabaseProjectDirectory, x.Value));
                                                       return item.Object;
                                                   })
                                                   .ToArray();

            ICollection<Error> loadErrors = new Collection<Error>();

            Mock<ITask> task = new Mock<ITask>(MockBehavior.Strict);
            Mock<IBuildEngine> buildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);

            task.SetupGet(x => x.BuildEngine).Returns(buildEngine.Object);
            buildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()));
            errorReporter.Setup(x => x.RegisterError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                         .Callback((string fileName, int line, int column, string errorNumber, string errorText) => loadErrors.Add(new Error(fileName, line, column, errorNumber, errorText)));
            errorReporter.SetupGet(x => x.HasErrors)
                         .Returns(() =>
                         {
                             if (!loadErrors.Any())
                                 return false;

                             Assert.True(false, String.Join(Environment.NewLine, loadErrors));
                             return true;
                         });

            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create("dbx", base.DatabaseSchemaProviderName, base.ModelCollation, source, new ITaskItem[0], task.Object, errorReporter.Object);
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