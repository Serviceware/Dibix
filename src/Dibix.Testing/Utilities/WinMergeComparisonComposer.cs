using System;
using System.IO;

namespace Dibix.Testing
{
    internal class WinMergeComparisonComposer
    {
        private static readonly object GlobalWinMergeBatchFileLock = new object();
        private const string ExpectedDirectoryName = "expected";
        private const string ActualDirectoryName = "actual";
        private readonly TestMethodTestResultFileComposer _testMethodFileComposer;
        private readonly TestRunTestResultFileComposer _testRunFileComposer;
        private readonly string _expectedDirectory;
        private readonly string _actualDirectory;

        public WinMergeComparisonComposer(TestMethodTestResultFileComposer testMethodFileComposer, TestRunTestResultFileComposer testRunFileComposer)
        {
            _testMethodFileComposer = testMethodFileComposer;
            _testRunFileComposer = testRunFileComposer;
            _expectedDirectory = Path.Combine(testRunFileComposer.ResultDirectory, ExpectedDirectoryName);
            _actualDirectory = Path.Combine(testRunFileComposer.ResultDirectory, ActualDirectoryName);
        }

        public void AddFileComparison(string expectedContent, string actualContent, string outputName, string extension)
        {
            EnsureFileComparisonContent(_expectedDirectory, outputName, extension, expectedContent);
            EnsureFileComparisonContent(_actualDirectory, outputName, extension, actualContent);
            EnsureWinMergeStarter();
        }

        private void EnsureWinMergeStarter()
        {
            lock (GlobalWinMergeBatchFileLock)
            {
                const string fileName = "winmerge.bat";
                if (_testRunFileComposer.IsFileNameRegistered(fileName))
                    return;

                _testRunFileComposer.AddResultFile(fileName, $"""
                                                                 @echo off
                                                                 start winmergeU "{ExpectedDirectoryName}" "{ActualDirectoryName}"
                                                                 """);
            }
        }

        private void EnsureFileComparisonContent(string path, string outputName, string extension, string content)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            string fileName = outputName;
            string alternativeFileName = directory.Name;

            if (!String.IsNullOrEmpty(extension))
            {
                fileName = $"{fileName}.{extension}";
                alternativeFileName = $"{alternativeFileName}.{extension}";
            }

            EnsureFileComparisonContent(Path.Combine(path, fileName), content);
            EnsureFileComparisonContent(Path.Combine(_testMethodFileComposer.ResultDirectory, alternativeFileName), content);
        }
        private void EnsureFileComparisonContent(string path, string content)
        {
            TestResultFileComposer.WriteContentToFile(path, content);
            _testMethodFileComposer.RegisterResultFile(path);
        }
    }
}