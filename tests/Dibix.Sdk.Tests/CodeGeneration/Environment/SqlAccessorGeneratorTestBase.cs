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

        protected void RunGeneratorTest(Action<ISqlAccessorGeneratorBuilder> configuration)
        {
            string testName = DetermineTestName();
            RunGeneratorTest(configuration, ProjectName, Namespace, testName, GetExpectedText(testName));
        }
        protected void RunGeneratorTest(string expectedTextKey, Action<ISqlAccessorGeneratorBuilder> configuration)
        {
            RunGeneratorTest(configuration, ProjectName, Namespace, DetermineTestName(), GetExpectedText(expectedTextKey));
        }
        protected void RunGeneratorTest(string expectedTextKey, string configurationJson)
        {
            RunGeneratorTest(configurationJson, ProjectName, Namespace, DetermineTestName(), GetExpectedText(expectedTextKey));
        }

        private static void RunGeneratorTest(Action<ISqlAccessorGeneratorBuilder> configuration, string projectName, string @namespace, string className, string expectedText)
        {
            TestUtilities.OverrideNamingConventions();
            ISqlAccessorGeneratorBuilder builder = SqlAccessorGeneratorFactory.Create(new TestExecutionEnvironment(projectName, @namespace, className)).Build();
            configuration(builder);
            RunGeneratorTest(builder.Generate, expectedText);
        }
        private static void RunGeneratorTest(string configurationJson, string projectName, string @namespace, string className, string expectedText)
        {
            TestUtilities.OverrideNamingConventions();
            string actualText = SqlAccessorGeneratorFactory.Create(new TestExecutionEnvironment(projectName, @namespace, className)).ParseJson(configurationJson);
            RunGeneratorTest(() => actualText, expectedText);
        }
        private static void RunGeneratorTest(Func<string> generator, string expectedText)
        {
            TestUtilities.OverrideNamingConventions();
            string actualText = generator();
            TestUtilities.AssertEqualWithDiffTool(expectedText, actualText);
        }

        private static string DetermineTestName() => new StackTrace().GetFrame(2).GetMethod().Name;

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
