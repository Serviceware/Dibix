using System.Diagnostics;
using System.IO;
using Xunit;

namespace Dibix.Sdk.Tests.Utilities
{
    public static class TestUtilities
    {
        public static void AssertEqualWithDiffTool(string expected, string actual)
        {
            if (expected != actual)
                RunWinMerge(expected, actual);

            Assert.Equal(expected, actual);
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
