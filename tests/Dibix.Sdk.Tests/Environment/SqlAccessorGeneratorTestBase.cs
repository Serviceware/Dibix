﻿using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Dibix.Sdk.Tests
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

        protected static void AssertAreEqual(string expected, string actual)
        {
            if (expected != actual)
                RunWinMerge(expected, actual);

            Assert.Equal(expected, actual);
        }

        private static void RunGeneratorTest(Action<ISqlAccessorGenerator> configuration, string projectName, string @namespace, string className, string expectedText)
        {
            ISqlAccessorGenerator generator = SqlAccessorGenerator.Create(new TestExecutionEnvironment(projectName, @namespace, className));
            configuration(generator);
            string actualText = generator.Generate();
            AssertAreEqual(expectedText, actualText);
        }

        private static string DetermineTestName(int frames = 3) => new StackTrace().GetFrame(frames).GetMethod().Name;

        private static string GetExpectedText(string key)
        {
            string resource = Resource.ResourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }

        private static void RunWinMerge(string expectedText, string actualText)
        {
            string expectedFileName = Path.GetTempFileName();
            string actualFileName = Path.GetTempFileName();
            File.WriteAllText(expectedFileName, expectedText);
            File.WriteAllText(actualFileName, actualText);
            Process.Start("winmerge", $"\"{expectedFileName}\" \"{actualFileName}\"");
        }
    }
}
