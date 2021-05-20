using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class DiffToolUtility
    {
        private const string WinMergeProcessName = "winmergeU";

        public static string GenerateReferencingBatchFile(TestContext context, string expected, string actual, out bool privateResultsDirectorySpecified)
        {
            string privateResultsDirectory = context.GetPrivateResultsDirectory(out privateResultsDirectorySpecified);
            string publicResultsDirectory = context.GetPublicResultsDirectory();

            string expectedFileName = "expected.txt";
            string actualFileName = "actual.txt";

            string expectedPrivatePath = Path.Combine(privateResultsDirectory, expectedFileName);
            string actualPrivatePath = Path.Combine(privateResultsDirectory, actualFileName);
            string expectedPublicPath = Path.Combine(publicResultsDirectory, expectedFileName);
            string actualPublicPath = Path.Combine(publicResultsDirectory, actualFileName);

            File.WriteAllText(expectedPrivatePath, expected);
            File.WriteAllText(actualPrivatePath, actual);

            string batchPath = Path.Combine(privateResultsDirectory, "winmerge.bat");
            string content = $@"@echo off
start {WinMergeProcessName} ""{expectedPublicPath}"" ""{actualPublicPath}""";
            File.WriteAllText(batchPath, content);

            return batchPath;
        }

        private static string GenerateSelfContainedBatchFile(TestContext context, string expected, string actual)
        {
            string expectedPath = GenerateRandomFilePath(context, "expected_", "txt");
            string actualPath = GenerateRandomFilePath(context, "actual_", "txt");
            string batchPath = Path.Combine(context.TestRunResultsDirectory, $"{context.TestName}.bat");
            string content = $@"@echo off
if not exist ""{context.TestRunResultsDirectory}"" mkdir ""{context.TestRunResultsDirectory}""
{GenerateEchoStatements(expected, expectedPath)}
{GenerateEchoStatements(actual, actualPath)}
start {WinMergeProcessName} ""{expectedPath}"" ""{actualPath}""";
            File.WriteAllText(batchPath, content);

            return batchPath;
        }

        private static string GenerateRandomFilePath(TestContext context, string prefix, string extension) => Path.Combine(context.TestRunResultsDirectory, $"{prefix}{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}.{extension}");

        private static string GenerateTestNameFilePath(TestContext context, string directory, string suffix) => Path.Combine(directory, $"{context.TestName}_{suffix}.txt");

        private static string GenerateEchoStatements(string content, string path) => String.Join(Environment.NewLine, content.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select((x, i) => $"echo {x.Replace("<", "^<").Replace(">", "^>")} {(i == 0 ? ">" : ">>")} \"{path}\""));
    }
}