using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Tests.Utilities;
using Microsoft.Build.Framework;
using Moq;
using Xunit;
using StringWriter = System.IO.StringWriter;

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
            string databaseProjectDirectory = Path.GetFullPath(@"..\..\..\..\Dibix.Sdk.Tests.Database");
            string violationScriptPath = Path.Combine(databaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{ruleInstance.Id:D3}.sql");
            string databaseProjectPath = Path.Combine(databaseProjectDirectory, "Dibix.Sdk.Tests.Database.sqlproj");

            XDocument databaseProject = XDocument.Load(databaseProjectPath);
            XmlNamespaceManager mgr = new XmlNamespaceManager(new NameTable());
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            string databaseSchemaProviderName = (string)databaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:DSP)", mgr);
            string modelCollation = (string)databaseProject.XPathEvaluate("string(x:Project/x:PropertyGroup/x:ModelCollation)", mgr);
            ITaskItem[] source = ((IEnumerable)databaseProject.XPathEvaluate("x:Project/x:ItemGroup/x:Build/@Include", mgr))
                                                              .OfType<XAttribute>()
                                                              .Select(x =>
                                                              {
                                                                  Mock<ITaskItem> item = new Mock<ITaskItem>(MockBehavior.Strict);
                                                                  item.SetupGet(y => y.MetadataNames).Returns(new string[0]);
                                                                  item.Setup(y => y.GetMetadata("FullPath")).Returns(Path.Combine(databaseProjectDirectory, x.Value));
                                                                  return item.Object;
                                                              })
                                                              .ToArray();

            ICollection<CompilerError> loadErrors = new Collection<CompilerError>();

            Mock<ITask> task = new Mock<ITask>(MockBehavior.Strict);
            Mock<IBuildEngine> buildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);

            task.SetupGet(x => x.BuildEngine).Returns(buildEngine.Object);
            buildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()));
            errorReporter.Setup(x => x.RegisterError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                         .Callback((string fileName, int line, int column, string errorNumber, string errorText) => loadErrors.Add(new CompilerError(fileName, line, column, errorNumber, errorText)));
            errorReporter.SetupGet(x => x.HasErrors)
                         .Returns(() =>
                         {
                             if (!loadErrors.Any())
                                 return false;

                             Assert.True(false, String.Join(Environment.NewLine, loadErrors));
                             return true;
                         });

            IDictionary<string, Assembly> dependentAssemblies = LoadDependentAssemblies().ToDictionary(x => x.Key, x => x.Value);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => dependentAssemblies.TryGetValue(new AssemblyName(e.Name).Name, out Assembly assembly) ? assembly : null;
            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create("dbx", databaseSchemaProviderName, modelCollation, source, new ITaskItem[0], task.Object, errorReporter.Object);
            IEnumerable<SqlCodeAnalysisError> errors = engine.Analyze(violationScriptPath, ruleInstance);

            string actual = GenerateXmlFromResults(errors);
            TestUtilities.AssertEqualWithDiffTool(expected, actual);
        }

        private static IEnumerable<KeyValuePair<string, Assembly>> LoadDependentAssemblies()
        {
            Assembly currentAssembly = typeof(SqlCodeAnalysisRuleTestsBase).Assembly;
            string pattern = $"^{Regex.Escape($"{currentAssembly.GetName().Name}.")}[A-Za-z.]+{Regex.Escape(".dll")}$";
            foreach (string resourceName in currentAssembly.GetManifestResourceNames().Where(x => Regex.IsMatch(x, pattern)))
            {
                using (Stream sourceStream = currentAssembly.GetManifestResourceStream(resourceName))
                {
                    using (MemoryStream targetStream = new MemoryStream())
                    {
                        sourceStream.CopyTo(targetStream);
                        Assembly assembly = Assembly.Load(targetStream.ToArray());
                        yield return new KeyValuePair<string, Assembly>(assembly.GetName().Name, assembly);
                    }
                }
            }
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