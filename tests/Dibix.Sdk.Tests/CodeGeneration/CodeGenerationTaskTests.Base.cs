using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Dibix.Sdk.CodeGeneration;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests : DatabaseTestBase
    {
        private readonly ITestOutputHelper _output;

        private static string ProjectName { get; } = Assembly.GetName().Name;
        private static string TestName => DetermineTestName();

        public CodeGenerationTaskTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        private void ExecuteTest(string source, bool embedStatements = true) => this.ExecuteTest(embedStatements, source);
        private void ExecuteTest(bool embedStatements = true, params string[] sources) => this.ExecuteTest(sources, Enumerable.Empty<string>(), Enumerable.Empty<string>(), embedStatements, false, false, Enumerable.Empty<string>());
        private void ExecuteTest(string source, string contract, params string[] expectedAdditionalAssemblyReferences) => this.ExecuteTest(source, Enumerable.Repeat(contract, 1), expectedAdditionalAssemblyReferences);
        private void ExecuteTest(string source, IEnumerable<string> contracts, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(Enumerable.Repeat(source, 1), contracts, true, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> contracts) => this.ExecuteTest(Enumerable.Empty<string>(), contracts, Enumerable.Empty<string>(), true, true, false, Enumerable.Empty<string>());
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, bool embedStatements, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, Enumerable.Empty<string>(), embedStatements, false, false, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, endpoints, true, false, true, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, bool embedStatements, bool generateClient, bool assertOpenApi, IEnumerable<string> expectedAdditionalAssemblyReferences)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"dibix-sdk-tests-{Guid.NewGuid()}");
            string outputFilePath = Path.Combine(tempDirectory, "TestAccessor.cs");
            Directory.CreateDirectory(tempDirectory);

            StringBuilder errorOutput = new StringBuilder();

            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogMessage(It.IsAny<string>())).Callback<string>(this._output.WriteLine);
            logger.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                  .Callback((string code, string text, string source, int line, int column) => errorOutput.AppendLine(CanonicalLogFormat.ToErrorString(code, text, source, line, column)));
            logger.SetupGet(x => x.HasLoggedErrors).Returns(errorOutput.Length > 0);

            bool result = CodeGenerationTask.Execute
            (
                projectDirectory: DatabaseProjectDirectory
              , productName: "Dibix.Sdk"
              , areaName: "Tests"
              , defaultOutputFilePath: !generateClient ? outputFilePath : null
              , clientOutputFilePath: generateClient ? outputFilePath : null
              , source: sources.Select(ToTaskItem).ToArray()
              , contracts: contracts.Select(ToTaskItem)
              , endpoints: endpoints.Select(ToTaskItem)
              , references: Enumerable.Empty<TaskItem>()
              , embedStatements: embedStatements
              , databaseSchemaProviderName: this.DatabaseSchemaProviderName
              , modelCollation: this.ModelCollation
              , sqlReferencePath: Enumerable.Empty<TaskItem>()
              , logger: logger.Object
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            if (errorOutput.Length > 0)
                throw new CodeGenerationException(errorOutput.ToString());

            Assert.True(result, "MSBuild task result was false");
            Assert.Equal(expectedAdditionalAssemblyReferences, additionalAssemblyReferences);
            EvaluateFile(outputFilePath);

            if (assertOpenApi)
            {
                string openApiDocumentFilePath = Path.Combine(tempDirectory, "Tests.yml");
                EvaluateFile($"{TestName}_OpenApi", openApiDocumentFilePath);
            }
        }

        private void ExecuteTestAndExpectError(string source, string expectedException) => this.ExecuteTestAndExpectError(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), expectedException);
        private void ExecuteTestAndExpectError(IEnumerable<string> sources, IEnumerable<string> contracts, string expectedException)
        {
            try
            {
                this.ExecuteTest(sources, contracts, true, Enumerable.Empty<string>());
                Assert.True(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.Equal(expectedException, ex.Message);
            }
        }

        private static void Evaluate(string generated) => Evaluate(TestName, generated);
        private static void Evaluate(string expectedTextKey, string generated)
        {
            string expectedText = GetExpectedText(expectedTextKey);
            string actualText = generated;
            TestUtility.AssertEqualWithDiffTool(expectedText, actualText, "cs");
        }

        private static void EvaluateFile(string generatedFilePath) => EvaluateFile(TestName, generatedFilePath);
        private static void EvaluateFile(string expectedTextKey, string generatedFilePath)
        {
            string expectedText = GetExpectedText(expectedTextKey);
            TestUtility.AssertFileEqualWithDiffTool(expectedText, generatedFilePath);
        }

        private static string GetExpectedText(string key)
        {
            ResourceManager resourceManager = new ResourceManager($"{ProjectName}.Resource", Assembly);
            string resource = resourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }

        private static string DetermineTestName() => new StackTrace().GetFrames()
                                                                     .Select(x => x.GetMethod())
                                                                     .Where(x => x.IsDefined(typeof(FactAttribute)))
                                                                     .Select(x => x.Name)
                                                                     .Single();

        private static TaskItem ToTaskItem(string relativePath) => new TaskItem(relativePath) { ["FullPath"] = Path.Combine(DatabaseProjectDirectory, relativePath) };
    }
}