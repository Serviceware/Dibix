using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public abstract class CodeGenerationTestBase : DatabaseTestBase
    {
        protected static string ProjectName { get; } = Assembly.GetName().Name;
        protected static string TestName => DetermineTestName();

        protected static void Evaluate(string generated) => Evaluate(TestName, generated);
        protected static void Evaluate(string expectedTextKey, string generated)
        {
            string expectedText = GetExpectedText(expectedTextKey);
            string actualText = generated;
            TestUtility.AssertEqualWithDiffTool(expectedText, actualText, "cs");
        }

        protected static void EvaluateFile(string generatedFilePath)
        {
            string expectedText = GetExpectedText(TestName);
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
    }
}