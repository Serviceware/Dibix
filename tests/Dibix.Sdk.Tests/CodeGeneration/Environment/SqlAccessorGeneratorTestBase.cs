using System;
using System.Diagnostics;
using System.Resources;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Tests.Utilities;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public abstract class SqlAccessorGeneratorTestBase
    {
        private static readonly string ProjectName = typeof(SqlAccessorGeneratorTestBase).Assembly.GetName().Name;
        private static readonly string Namespace = typeof(SqlAccessorGeneratorTestBase).Assembly.GetName().Name;

        protected void RunGeneratorTest(Action<ISqlAccessorGenerator> configuration)
        {
            string testName = DetermineTestName(2);
            string expectedText = GetExpectedText(testName);
            RunGeneratorTest(configuration, ProjectName, Namespace, testName, expectedText);
        }

        protected static string DetermineExpectedText()
        {
            string key = DetermineTestName();
            return GetExpectedText(key);
        }

        private static void RunGeneratorTest(Action<ISqlAccessorGenerator> configuration, string projectName, string @namespace, string className, string expectedText)
        {
            ISqlAccessorGenerator generator = SqlAccessorGenerator.Create(new TestExecutionEnvironment(projectName, @namespace, className));
            configuration(generator);
            string actualText = generator.Generate();
            TestUtilities.AssertEqualWithDiffTool(expectedText, actualText);
        }

        private static string DetermineTestName(int frames = 3) => new StackTrace().GetFrame(frames).GetMethod().Name;

        private static string GetExpectedText(string key)
        {
            ResourceManager resourceManager = new ResourceManager("Dibix.Sdk.Tests.Resource", typeof(SqlAccessorGeneratorTestBase).Assembly);
            string resource = resourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }
    }
}
