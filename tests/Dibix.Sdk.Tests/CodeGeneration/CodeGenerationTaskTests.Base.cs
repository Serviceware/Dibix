﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests
    {
        private readonly ITestOutputHelper _output;

        public CodeGenerationTaskTests(ITestOutputHelper output) => this._output = output;

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

            TestLogger logger = new TestLogger(this._output);

            bool result = CodeGenerationTask.Execute
            (
                projectDirectory: DatabaseTestUtility.DatabaseProjectDirectory
              , productName: "Dibix.Sdk"
              , areaName: "Tests"
              , defaultOutputFilePath: !generateClient ? outputFilePath : null
              , clientOutputFilePath: generateClient ? outputFilePath : null
              , source: sources.Select(ToTaskItem).ToArray()
              , contracts: contracts.Select(ToTaskItem)
              , endpoints: endpoints.Select(ToTaskItem)
              , references: Enumerable.Empty<TaskItem>()
              , embedStatements: embedStatements
              , databaseSchemaProviderName: DatabaseTestUtility.DatabaseSchemaProviderName
              , modelCollation: DatabaseTestUtility.ModelCollation
              , sqlReferencePath: Enumerable.Empty<TaskItem>()
              , logger: logger
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            logger.Verify();

            Assert.True(result, "MSBuild task result was false");
            Assert.Equal(expectedAdditionalAssemblyReferences, additionalAssemblyReferences);
            EvaluateFile(outputFilePath);

            if (assertOpenApi)
            {
                string openApiDocumentFilePath = Path.Combine(tempDirectory, "Tests.yml");
                EvaluateFile($"{TestUtility.TestName}_OpenApi", openApiDocumentFilePath);
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

        private static void EvaluateFile(string generatedFilePath) => EvaluateFile(TestUtility.TestName, generatedFilePath);
        private static void EvaluateFile(string expectedTextKey, string generatedFilePath)
        {
            string expectedText = TestUtility.GetExpectedText(expectedTextKey);
            TestUtility.AssertFileEqualWithDiffTool(expectedText, generatedFilePath);
        }

        private static TaskItem ToTaskItem(string relativePath) => new TaskItem(relativePath) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, relativePath) };
    }
}