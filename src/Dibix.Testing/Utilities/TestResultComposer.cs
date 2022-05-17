using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestResultComposer
    {
        private readonly TestContext _testContext;
        private readonly string _runDirectory;
        private readonly string _testDirectory;
        private readonly IDictionary<string, ICollection<string>> _testNameFileMap;

        public TestResultComposer(TestContext testContext)
        {
            this._testContext = testContext;
            this._runDirectory = testContext.TestRunResultsDirectory;
            this._testDirectory = this.EnsureDirectory("TestResults", testContext.TestName);
            this._testNameFileMap = new Dictionary<string, ICollection<string>>();
        }

        public string AddFile(string fileName)
        {
            string path = Path.Combine(this._testDirectory, fileName);
            this.RegisterFile(path);
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
            this.EnsureFileComparisonContent(this.GetExpectedDirectory(), extension, expectedContent);
            this.EnsureFileComparisonContent(this.GetActualDirectory(), extension, actualContent);
            this.EnsureWinMergeStarter();
        }

        public void Complete()
        {
            if (!this._testNameFileMap.TryGetValue(this._testContext.TestName, out ICollection<string> files))
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
            this.RegisterFile(path);
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
            this.RegisterFile(path);
        }

        private string GetExpectedDirectory() => this.EnsureDirectory("expected");
        
        private string GetActualDirectory() => this.EnsureDirectory("actual");

        private string EnsureDirectory(params string[] directoryNames)
        {
            string path = Path.Combine(Enumerable.Repeat(this._runDirectory, 1).Concat(directoryNames).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        private void RegisterFile(string path)
        {
            this._testContext.AddResultFile(path);
            
            if (!this._testNameFileMap.TryGetValue(this._testContext.TestName, out ICollection<string> files))
            {
                files = new Collection<string>();
                this._testNameFileMap.Add(this._testContext.TestName, files);
            }
            files.Add(path);
        }

        private void EnsureWinMergeStarter()
        {
            const string fileName = "winmerge.bat";
            string path = Path.Combine(this._runDirectory, fileName);
            if (File.Exists(path))
                return;

            string expectedDirectory = new DirectoryInfo(this.GetExpectedDirectory()).Name;
            string actualDirectory = new DirectoryInfo(this.GetActualDirectory()).Name;

            WriteContentToFile(path, $@"@echo off
start winmergeU ""{expectedDirectory}"" ""{actualDirectory}""");
            this.RegisterFile(path);
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
    }
}