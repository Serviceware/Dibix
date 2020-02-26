using System.Diagnostics;
using System.IO;
using Xunit;

namespace Dibix.Sdk.Tests.Utilities
{
    public static class TestUtilities
    {
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
    }
}
