using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests : TestBase
    {
        private void ExecuteTest
        (
            IEnumerable<string> sources = null
          , IEnumerable<string> contracts = null
          , IEnumerable<string> endpoints = null
          , bool isEmbedded = true
          , bool enableExperimentalFeatures = false
          , AssertOutputKind outputKind = AssertOutputKind.Accessor
          , IEnumerable<string> expectedAdditionalAssemblyReferences = null
        )
        {
            string outputDirectory = Path.Combine(base.TestContext.TestRunResultsDirectory, "Output", base.TestContext.TestName);
            Directory.CreateDirectory(outputDirectory);
            const string areaName = "Tests";
            const string defaultOutputName = "TestAccessor";
            const string clientOutputName = "TestAccessor.Client";

            ICollection<TaskItem> endpointItems = (endpoints ?? Enumerable.Empty<string>()).Select(ToTaskItem).ToArray();
            TestLogger logger = new TestLogger(base.Out, distinctErrorLogging: true);

            bool result = SqlCoreTask.Execute
            (
                projectName: DatabaseTestUtility.ProjectName
              , projectDirectory: DatabaseTestUtility.DatabaseProjectDirectory
              , configurationFilePath: Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "dibix.json")
              , staticCodeAnalysisSucceededFile: null
              , resultsFile: null
              , productName: "Dibix.Sdk"
              , areaName: areaName
              , title: "Dibix.Sdk.Tests API title"
              , version: "1.0.1"
              , description: "Dibix.Sdk.Tests API description"
              , outputDirectory: outputDirectory
              , defaultOutputName: defaultOutputName
              , clientOutputName: outputKind == AssertOutputKind.Client ? clientOutputName : null
              , externalAssemblyReferenceDirectory: Environment.CurrentDirectory
              , source: (sources ?? Enumerable.Empty<string>()).Select(ToTaskItem).ToArray()
              , scriptSource: Enumerable.Empty<TaskItem>()
              , contracts: (contracts ?? Enumerable.Empty<string>()).Select(ToTaskItem)
              , endpoints: endpointItems
              , references: Enumerable.Empty<TaskItem>()
              , defaultSecuritySchemes: new[] { "HLNS-SIT", "HLNS-ClientId" }.Select(ToTaskItem)
              , isEmbedded: isEmbedded
              , limitDdlStatements: true
              , preventDmlReferences: true
              , enableExperimentalFeatures: enableExperimentalFeatures // TODO: Add test support for inspecting DBX file
              , databaseSchemaProviderName: DatabaseTestUtility.DatabaseSchemaProviderName
              , modelCollation: DatabaseTestUtility.ModelCollation
              , sqlReferencePath: Array.Empty<TaskItem>()
              , logger: logger
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            logger.Verify();

            Assert.IsTrue(result, "MSBuild task result was false");
            AssertAreEqual(expectedAdditionalAssemblyReferences ?? Enumerable.Empty<string>(), additionalAssemblyReferences);

            bool hasEndpoints = endpointItems.Any();
            string endpointOutputFilePath = Path.Combine(outputDirectory, $"{areaName}.cs");
            Assert.AreEqual(hasEndpoints, File.Exists(endpointOutputFilePath), "hasEndpoints == File.Exists(endpointOutputFilePath)");

            switch (outputKind)
            {
                case AssertOutputKind.None:
                    break;

                case AssertOutputKind.Accessor:
                    string accessorOutputFilePath = Path.Combine(outputDirectory, $"{defaultOutputName}.Accessor.cs");
                    this.AssertFile(accessorOutputFilePath);
                    break;

                case AssertOutputKind.Endpoint:
                    this.AssertFile(endpointOutputFilePath);
                    break;

                case AssertOutputKind.Client:
                    string clientOutputFilePath = Path.Combine(outputDirectory, $"{clientOutputName}.cs");
                    this.AssertFile(clientOutputFilePath);
                    break;

                case AssertOutputKind.OpenApi:
                    string openApiDocumentFilePath = Path.Combine(outputDirectory, "Tests.yml");

                    // The OpenAPI SDK uses LF but even if we would use LF in the expected file, Git's autocrlf feature would mess it up again
                    const bool normalizeLineEndings = true;
                    this.AssertFileContent(openApiDocumentFilePath, normalizeLineEndings);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(outputKind), outputKind, null);
            }
        }

        private void ExecuteTestAndExpectError
        (
            string expectedException
          , IEnumerable<string> sources = null
          , IEnumerable<string> contracts = null
          , IEnumerable<string> endpoints = null
          , bool isEmbedded = true
        )
        {
            try
            {
                this.ExecuteTest(sources, contracts, endpoints, isEmbedded);
                Assert.IsTrue(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.AreEqual(expectedException, ex.Message);
            }
        }

        private void AssertFile(string generatedFilePath)
        {
            this.AssertFileContent(generatedFilePath);
            AssertFileCompilation(generatedFilePath);
        }

        private void AssertFileContent(string generatedFilePath, bool normalizeLineEndings = false) => this.AssertFileContent(base.TestContext.TestName, generatedFilePath, normalizeLineEndings);
        private void AssertFileContent(string expectedTextKey, string generatedFilePath, bool normalizeLineEndings = false)
        {
            string actualText = Regex.Replace(File.ReadAllText(generatedFilePath)
                                            , @"This code was generated by Dibix SDK [\d]+\.[\d]+\.[\d]+\.[\d]+\."
                                            , "This code was generated by Dibix SDK 1.0.0.0.");
            string extension = Path.GetExtension(generatedFilePath).TrimStart('.');
            string resourceKey = ResourceUtility.BuildResourceKey($"CodeGeneration.{expectedTextKey}.{extension}");
            string expectedText = base.GetEmbeddedResourceContent(resourceKey);
            base.AssertEqual(expectedText, actualText, extension, normalizeLineEndings: normalizeLineEndings);
        }

        private static void AssertFileCompilation(string generatedFilePath)
        {
            using (Stream inputStream = File.OpenRead(generatedFilePath))
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(inputStream), path: generatedFilePath);
                CSharpCompilation compilation = CSharpCompilation.Create(null)
                                                                 .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.Http.Client.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.Http.Server.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Newtonsoft.Json.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("System.Net.Http.Formatting.dll"))
                                                                 .AddReference<Object>()
                                                                 .AddReference<System.Runtime.Serialization.DataContractAttribute>()
                                                                 .AddReference<System.ComponentModel.DataAnnotations.KeyAttribute>()
                                                                 .AddReference<System.Data.CommandType>()
                                                                 .AddReference<System.Linq.Expressions.Expression>()
                                                                 .AddReference<System.Net.Http.HttpClient>()
                                                                 .AddReference<Uri>()
                                                                 .AddSyntaxTrees(syntaxTree);

                RoslynUtility.VerifyCompilation(compilation);
            }
        }

        private static TaskItem ToTaskItem(string relativePath) => new TaskItem(relativePath) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, relativePath) };

        public enum AssertOutputKind
        {
            None,
            Accessor,
            Endpoint,
            Client,
            OpenApi
        }
    }
}