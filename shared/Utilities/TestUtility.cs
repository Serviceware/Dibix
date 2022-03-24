using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dibix
{
    internal static class TestUtility
    {
        public static Assembly Assembly { get; } = typeof(TestUtility).Assembly;
        public static string ProjectName { get; } = Assembly.GetName().Name;
        public static string TestName => DetermineTestName();

        public static void Evaluate(string generated) => Evaluate(TestName, generated);
        public static void Evaluate(string expectedTextKey, string generated)
        {
            const string extension = "cs";
            string resourceKey = $"{expectedTextKey}.{extension}";
            string expectedText = TestUtility.GetExpectedText(resourceKey);
            string actualText = generated;
            TestUtility.AssertEqualWithDiffTool(expectedText, actualText, extension);
        }
        
        public static string GetExpectedText(string key)
        {
            string resourceKey = $"{Assembly.GetName().Name}.Resources.{key}";
            using (Stream stream = Assembly.GetManifestResourceStream(resourceKey))
            {
                if (stream == null)
                    throw new InvalidOperationException($@"Resource not found: {resourceKey}
{Assembly.Location}");

                using (TextReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }

        public static void AssertEqualWithDiffTool(string expectedText, string actualText, string extension = null)
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
            string actualFilePath = Path.GetTempFileName();
            if (extension != null)
                actualFilePath = Path.ChangeExtension(actualFilePath, extension);
            
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
            Process.Start("WinMergeU", $"\"{expectedFilePath}\" \"{actualFilePath}\"");
        }

        private static string DetermineTestName() => new StackTrace().GetFrames()
                                                                     .Select(x => x.GetMethod())
                                                                     .Where(x => x.IsDefined(typeof(FactAttribute)))
                                                                     .Select(x => x.Name)
                                                                     .Single();
    }
}