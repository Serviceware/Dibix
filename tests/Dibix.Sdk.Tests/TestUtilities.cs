using System.Diagnostics;
using System.IO;
using Xunit;

namespace Dibix.Sdk.Tests.Utilities
{
    public static class TestUtilities
    {
        public static void AssertEqualWithDiffTool(string expectedText, string actualText)
        {
            if (expectedText != actualText)
                RunWinMerge(expectedText, actualText);

            Assert.Equal(expectedText, actualText);
        }

        public static void AssertFileEqualWithDiffTool(string expectedText, string actualFilePath)
        {
            string actualText = File.ReadAllText(actualFilePath);
            if (expectedText != actualText)
                RunWinMerge(expectedText, actualText);

            Assert.Equal(expectedText, actualText);
        }

        private static void RunWinMerge(string expectedText, string actualText)
        {
            string actualFilePath = Path.GetTempFileName();
            File.WriteAllText(actualFilePath, actualText);
            RunWinMerge(expectedText, actualFilePath, actualText);
        }

        private static void RunWinMerge(string expectedText, string actualFilePath, string actualText)
        {
            string expectedFilePath = Path.GetTempFileName();
            File.WriteAllText(expectedFilePath, expectedText);
            File.WriteAllText(actualFilePath, actualText);
            Process.Start("winmerge", $"\"{expectedFilePath}\" \"{actualFilePath}\"");
        }
    }
}
