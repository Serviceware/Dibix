﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests
{
    internal static class TestUtility
    {
        public static Assembly Assembly { get; } = typeof(TestUtility).Assembly;
        public static string ProjectName { get; } = Assembly.GetName().Name;
        public static string TestName => DetermineTestName();

        public static void Evaluate(string generated) => Evaluate(TestName, generated);
        public static void Evaluate(string expectedTextKey, string generated)
        {
            string expectedText = TestUtility.GetExpectedText(expectedTextKey);
            string actualText = generated;
            TestUtility.AssertEqualWithDiffTool(expectedText, actualText, "cs");
        }

        public static string GetExpectedText(string key)
        {
            ResourceManager resourceManager = new ResourceManager($"{ProjectName}.Resource", Assembly);
            string resource = resourceManager.GetString(key);
            if (resource == null)
                throw new InvalidOperationException($"Invalid test resource name '{key}'");

            return resource;
        }

        public static void AssertEqualWithDiffTool(string expectedText, string actualText, string extension)
        {
            if (expectedText != actualText)
                RunDiffTool(expectedText, actualText, extension);

            Assert.Equal(expectedText, actualText);
        }

        public static void AssertFileEqualWithDiffTool(string expectedText, string actualFilePath)
        {
            string actualText = File.ReadAllText(actualFilePath);
            if (expectedText != actualText)
                RunWinMerge(expectedText, actualFilePath);

            Assert.Equal(expectedText, actualText);
        }

        private static void RunDiffTool(string expectedText, string actualText, string extension)
        {
            string actualFilePath = Path.ChangeExtension(Path.GetTempFileName(), extension);
            File.WriteAllText(actualFilePath, actualText);
            RunWinMerge(expectedText, actualFilePath);
        }

        private static void RunWinMerge(string expectedText, string actualFilePath)
        {
            string expectedFilePath = Path.GetTempFileName();

            string expectedFilePathExtension = Path.GetExtension(expectedFilePath);
            string actualFilePathExtension = Path.GetExtension(actualFilePath);
            if (expectedFilePathExtension != actualFilePathExtension)
                expectedFilePath = Path.ChangeExtension(expectedFilePath, actualFilePathExtension);

            File.WriteAllText(expectedFilePath, expectedText);
            Process.Start("winmerge", $"\"{expectedFilePath}\" \"{actualFilePath}\"");
        }

        private static string DetermineTestName() => new StackTrace().GetFrames()
                                                                     .Select(x => x.GetMethod())
                                                                     .Where(x => x.IsDefined(typeof(FactAttribute)))
                                                                     .Select(x => x.Name)
                                                                     .Single();
    }
}