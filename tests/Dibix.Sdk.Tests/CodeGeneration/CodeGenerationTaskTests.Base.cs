using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dibix.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests : TestBase
    {
        private void ExecuteTest(string source, bool isEmbedded = true) => this.ExecuteTest(isEmbedded, source);
        private void ExecuteTest(bool isEmbedded = true, params string[] sources) => this.ExecuteTest(sources, Enumerable.Empty<string>(), Enumerable.Empty<string>(), isEmbedded, false, false, Enumerable.Empty<string>());
        private void ExecuteTest(bool isEmbedded, IEnumerable<string> contracts, params string[] sources) => this.ExecuteTest(sources, contracts, Enumerable.Empty<string>(), isEmbedded, false, false, Enumerable.Empty<string>());
        private void ExecuteTest(string source, string contract, params string[] expectedAdditionalAssemblyReferences) => this.ExecuteTest(source, Enumerable.Repeat(contract, 1), expectedAdditionalAssemblyReferences);
        private void ExecuteTest(string source, IEnumerable<string> contracts, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(Enumerable.Repeat(source, 1), contracts, Enumerable.Empty<string>(), true, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, bool isEmbedded, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, endpoints, isEmbedded, false, false, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(false, sources, contracts, endpoints, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(bool generateClient, IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, endpoints, false, generateClient, !generateClient, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, bool isEmbedded, bool generateClient, bool assertOpenApi, IEnumerable<string> expectedAdditionalAssemblyReferences)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"dibix-sdk-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDirectory);
            const string defaultOutputName = "TestAccessor";
            const string clientOutputName = "TestAccessor.Client";
            string defaultOutputFilePath = Path.Combine(tempDirectory, $"{defaultOutputName}.Accessor.cs");
            string clientOutputFilePath = Path.Combine(tempDirectory, $"{clientOutputName}.cs");

            TestLogger logger = new TestLogger(base.Out, distinctErrorLogging: true);

            bool result = SqlCoreTask.Execute
            (
                projectName: DatabaseTestUtility.ProjectName
              , projectDirectory: DatabaseTestUtility.DatabaseProjectDirectory
              , configurationFilePath: Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "dibix.json")
              , staticCodeAnalysisSucceededFile: null
              , resultsFile: null
              , productName: "Dibix.Sdk"
              , areaName: "Tests"
              , title: "Dibix.Sdk.Tests API title"
              , version: "1.0.1"
              , description: "Dibix.Sdk.Tests API description"
              , outputDirectory: tempDirectory
              , defaultOutputName: defaultOutputName
              , clientOutputName: generateClient ? clientOutputName : null
              , externalAssemblyReferenceDirectory: Environment.CurrentDirectory
              , source: sources.Select(ToTaskItem).ToArray()
              , scriptSource: Enumerable.Empty<TaskItem>()
              , contracts: contracts.Select(ToTaskItem)
              , endpoints: endpoints.Select(ToTaskItem)
              , references: Enumerable.Empty<TaskItem>()
              , defaultSecuritySchemes: new[] { "HLNS-SIT", "HLNS-ClientId" }.Select(ToTaskItem)
              , isEmbedded: isEmbedded
              , enableExperimentalFeatures: false // TODO: Add test support for inspecting DBX file
              , databaseSchemaProviderName: DatabaseTestUtility.DatabaseSchemaProviderName
              , modelCollation: DatabaseTestUtility.ModelCollation
              , sqlReferencePath: Array.Empty<TaskItem>()
              , logger: logger
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            logger.Verify();

            Assert.IsTrue(result, "MSBuild task result was false");
            AssertAreEqual(expectedAdditionalAssemblyReferences, additionalAssemblyReferences);
            this.AssertFile(generateClient ? clientOutputFilePath : defaultOutputFilePath);

            if (assertOpenApi)
            {
                string openApiDocumentFilePath = Path.Combine(tempDirectory, "Tests.yml");

                // The OpenAPI SDK uses LF but even if we would use LF in the expected file, Git's autocrlf feature would mess it up again
                const bool normalizeLineEndings = true;
                this.AssertFileContent(openApiDocumentFilePath, normalizeLineEndings);
            }
        }

        private void ExecuteTestAndExpectError(string source, string expectedException) => this.ExecuteTestAndExpectError(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), Enumerable.Empty<string>(), expectedException);
        private void ExecuteTestAndExpectError(IEnumerable<string> contracts, string expectedException) => this.ExecuteTestAndExpectError(Enumerable.Empty<string>(), contracts, Enumerable.Empty<string>(), expectedException);
        private void ExecuteTestAndExpectError(string source, string endpoint, string expectedException) => this.ExecuteTestAndExpectError(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), Enumerable.Repeat(endpoint, 1), expectedException);
        private void ExecuteTestAndExpectError(IEnumerable<string> sources, IEnumerable<string> contracts, string endpoint, string expectedException) => this.ExecuteTestAndExpectError(sources, contracts, Enumerable.Repeat(endpoint, 1), expectedException);
        private void ExecuteTestAndExpectError(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, string expectedException)
        {
            try
            {
                this.ExecuteTest(sources, contracts, endpoints, isEmbedded: false, expectedAdditionalAssemblyReferences: Enumerable.Empty<string>());
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

        private void AssertFileContent(string generatedFilePath, bool normalizeLineEndings = false) => AssertFileContent(base.TestContext.TestName, generatedFilePath, normalizeLineEndings);
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
                                                                 .AddReferences(ResolveReference<Object>())
                                                                 .AddReferences(ResolveReference<System.Runtime.Serialization.DataContractAttribute>())
                                                                 .AddReferences(ResolveReference<System.ComponentModel.DataAnnotations.KeyAttribute>())
                                                                 .AddReferences(ResolveReference<System.Data.CommandType>())
                                                                 .AddReferences(ResolveReference<System.Linq.Expressions.Expression>())
                                                                 .AddReferences(ResolveReference<System.Net.Http.HttpClient>())
                                                                 .AddReferences(ResolveReference<Uri>())
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.Http.Client.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Dibix.Http.Server.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("Newtonsoft.Json.dll"))
                                                                 .AddReferences(MetadataReference.CreateFromFile("System.Net.Http.Formatting.dll"))
                                                                 .AddSyntaxTrees(syntaxTree);

                using (Stream outputStream = new MemoryStream())
                {
                    EmitResult emitResult = compilation.Emit(outputStream);
                    if (emitResult.Success) 
                        return;

                    StringBuilder sb = new StringBuilder();
                    foreach (Diagnostic error in emitResult.Diagnostics)
                        sb.AppendLine(error.ToString());

                    throw new CodeCompilationException(sb.ToString());
                }
            }
        }

        private static MetadataReference ResolveReference<T>() => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);

        private static TaskItem ToTaskItem(string relativePath) => new TaskItem(relativePath) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, relativePath) };
    }
}