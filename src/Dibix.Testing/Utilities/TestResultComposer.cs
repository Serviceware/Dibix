using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestResultComposer
    {
        private const string ExpectedDirectoryName = "expected";
        private const string ActualDirectoryName = "actual";
        private readonly TestContext _testContext;
        private readonly bool _useDedicatedTestResultsDirectory;
        private readonly string _defaultRunDirectory;
        private readonly string _runDirectory;
        private readonly string _testDirectory;
        private readonly string _expectedDirectory;
        private readonly string _actualDirectory;
        private readonly ICollection<string> _testRunFiles;
        private readonly ICollection<string> _testFiles;

        public TestResultComposer(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            this._testContext = testContext;
            this._useDedicatedTestResultsDirectory = useDedicatedTestResultsDirectory;
            this._defaultRunDirectory = testContext.TestRunResultsDirectory;
            string dedicatedRunDirectory = BuildDedicatedRunDirectory(testContext);
            this._runDirectory = this._useDedicatedTestResultsDirectory ? dedicatedRunDirectory : this._defaultRunDirectory;
            this._testDirectory = Path.Combine(this._runDirectory, "TestResults", testContext.TestName);
            this._expectedDirectory = Path.Combine(this._runDirectory, ExpectedDirectoryName);
            this._actualDirectory = Path.Combine(this._runDirectory, ActualDirectoryName);
            this._testRunFiles = new HashSet<string>();
            this._testFiles = new HashSet<string>();
        }

        public string AddFile(string fileName)
        {
            string path = Path.Combine(this._testDirectory, fileName);
            this.RegisterFile(path, scopeIsTestRun: false);
            return path;
        }
        public string AddFile(string fileName, string content)
        {
            string path = this.AddFile(fileName);
            WriteContentToFile(path, content);
            return path;
        }

        public void AddFileComparison(string expectedContent, string actualContent, string extension)
        {
            this.EnsureFileComparisonContent(this._expectedDirectory, extension, expectedContent);
            this.EnsureFileComparisonContent(this._actualDirectory, extension, actualContent);
            this.EnsureWinMergeStarter();
        }

        public void Complete()
        {
            this.ZipTestOutput();
            this.CopyTestOutput();
        }

#if NETCOREAPP
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public void AddLastEventLogErrors(int count = 5) => this.AddLastEventLogEntries(EventLogEntryType.Error, count);

        private void EnsureFileComparisonContent(string directory, string extension, string content)
        {
            this.EnsureFileComparisonContent(Path.Combine(directory, $"{this._testContext.TestName}.{extension}"), content);
            this.EnsureFileComparisonContent(Path.Combine(this._testDirectory, $"{new DirectoryInfo(directory).Name}.{extension}"), content);
        }
        private void EnsureFileComparisonContent(string path, string content)
        {
            WriteContentToFile(path, content);
            this.RegisterFile(path, scopeIsTestRun: false);
        }

        private bool ShouldRegisterTestRunFile(string path)
        {
            if (File.Exists(path))
            {
                // Ultimately, test run files are collected once the whole test run is completed.
                // Unfortunately, there is no easy way for us to execute code at this level apart from AssemblyCleanup.
                // However the AssemblyCleanup method doesn't accept a TestContext and is also not inheritable.
                // Therefore this step is executed after each test and involves to skip these files in subsequent tests of the current run.
                this._testRunFiles.Add(path);
                return false;
            }

            return true;
        }

        private void RegisterFile(string path, bool scopeIsTestRun)
        {
            ICollection<string> files = scopeIsTestRun ? this._testRunFiles : this._testFiles;
            if (files.Contains(path))
                throw new InvalidOperationException($"Test result file already registered: {path}");

            EnsureDirectory(path);
            this._testContext.AddResultFile(path);
            files.Add(path);
        }

        private void EnsureWinMergeStarter()
        {
            const string fileName = "winmerge.bat";
            string path = Path.Combine(this._runDirectory, fileName);

            if (!this.ShouldRegisterTestRunFile(path))
                return;

            WriteContentToFile(path, $@"@echo off
start winmergeU ""{ExpectedDirectoryName}"" ""{ActualDirectoryName}""");
            this.RegisterFile(path, scopeIsTestRun: true);
        }

#if NETCOREAPP
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        private void AddLastEventLogEntries(EventLogEntryType eventLogEntryType, int count)
        {
            EventLog eventLog = new EventLog("Application");

            Enumerable.Range(0, eventLog.Entries.Count)
                      .Reverse()
                      .Select(x => eventLog.Entries[x])
                      .Where(x => x.EntryType == eventLogEntryType)
                      .Take(count)
                      .Each((x, i) => this.AddFile($"EventLog{eventLogEntryType}_{i}.txt", $@"{x.TimeGenerated:O} - {x.EntryType} - {x.Source}
---
{x.Message}"));
        }

        private static void WriteContentToFile(string path, string content)
        {
            if (File.Exists(path))
                throw new InvalidOperationException($"Path exists already: {path}");

            File.WriteAllText(path, content);
        }

        private void ZipTestOutput()
        {
            ICollection<string> files = this._testRunFiles.Concat(this._testFiles).ToArray();
            if (!files.Any())
                return;

            string path = Path.Combine(this._testDirectory, $"{this._testContext.TestName}.zip");
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    int relativePathIndex = this._runDirectory.Length + 1;
                    string relativePath = file.Substring(relativePathIndex, file.Length - relativePathIndex);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            }

            this.RegisterFile(path, scopeIsTestRun: false);
        }

        private void CopyTestOutput()
        {
            if (!this._useDedicatedTestResultsDirectory)
                return;

            this.CopyFiles(this._testRunFiles, this._defaultRunDirectory, ignoreIfExists: true);
            this.CopyFiles(this._testFiles, this._defaultRunDirectory);
        }

        private void CopyFiles(IEnumerable<string> files, string targetDirectory, bool ignoreIfExists = false)
        {
            foreach (string file in files)
            {
                string relativeFilePath = file.Substring(this._runDirectory.Length + 1);
                string targetFilePath = Path.Combine(targetDirectory, relativeFilePath);
                EnsureDirectory(targetFilePath);

                if (!ignoreIfExists || !File.Exists(targetFilePath))
                    File.Copy(file, targetFilePath);
            }
        }

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
        }

        private static string BuildDedicatedRunDirectory(TestContext testContext)
        {
            Assembly assembly = TestImplementationResolver.ResolveTestAssembly(testContext);
            string assemblyName = assembly.GetName().Name;
            string directoryName = new DirectoryInfo(testContext.TestRunDirectory).Name;
            string path = Path.Combine(Path.GetTempPath(), assemblyName, directoryName);
            return path;
        }
    }
}