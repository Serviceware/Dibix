using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dibix.Testing
{
    internal sealed class TestResultComposer
    {
        private const string ExpectedDirectoryName = "expected";
        private const string ActualDirectoryName = "actual";
        private readonly TestContext _testContext;
        private readonly bool _useDedicatedTestResultsDirectory;
        private readonly string _defaultRunDirectory;
        private readonly string _expectedDirectory;
        private readonly string _normalizedTestName;
        private readonly string _actualDirectory;
        private readonly ICollection<string> _testRunFiles;
        private readonly ICollection<string> _testFiles;
        private bool _eventLogCollected;

        public string RunDirectory { get; }
        public string TestRootDirectory { get; }
        public string TestDirectory { get; }

        public TestResultComposer(TestContext testContext, bool useDedicatedTestResultsDirectory)
        {
            this._testContext = testContext;
            this._useDedicatedTestResultsDirectory = useDedicatedTestResultsDirectory;
            this._defaultRunDirectory = testContext.TestRunResultsDirectory;
            this.RunDirectory = this._useDedicatedTestResultsDirectory ? TestContextUtility.GetTestRunDirectory(testContext) : this._defaultRunDirectory;
            this.TestRootDirectory = Path.Combine(this.RunDirectory, "Tests");
            this._normalizedTestName = String.Join("_", TestContextUtility.GetTestName(testContext).Split(Path.GetInvalidFileNameChars()));
            this.TestDirectory = Path.Combine(this.TestRootDirectory, _normalizedTestName);
            this._expectedDirectory = Path.Combine(this.RunDirectory, ExpectedDirectoryName);
            this._actualDirectory = Path.Combine(this.RunDirectory, ActualDirectoryName);
            this._testRunFiles = new HashSet<string>();
            this._testFiles = new HashSet<string>();
            this.EnsureTestContextDump();
        }

        public string AddFile(string fileName)
        {
            string path = Path.Combine(this.TestDirectory, fileName);
            EnsureDirectory(path);
            this.RegisterFile(path, scopeIsTestRun: false);
            return path;
        }
        public string AddFile(string fileName, string content)
        {
            string path = this.AddFile(fileName);
            WriteContentToFile(path, content);
            return path;
        }

        public void AddFileComparison(string expectedContent, string actualContent, string outputName, string extension)
        {
            this.EnsureFileComparisonContent(this._expectedDirectory, outputName, extension, expectedContent);
            this.EnsureFileComparisonContent(this._actualDirectory, outputName, extension, actualContent);
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
        public void AddLastEventLogEntries(EventLogDiagnosticsOptions options)
        {
            string FormatContent(EventLogEntry entry)
            {
                string title = $" {entry.TimeGenerated:O} [{entry.EntryType}] {entry.Source} ";
                string border = new string('=', title.Length);
                return $@"{border}
{title}
{border}
{entry.Message}";
            }

            if (_eventLogCollected)
                return;

            _eventLogCollected = true;

            AddFile("EventLogOptions.json", JsonConvert.SerializeObject(options, Formatting.Indented));

            EventLog eventLog = new EventLog(options.LogName, options.MachineName, options.Source);

            Enumerable.Range(0, eventLog.Entries.Count)
                      .Reverse()
                      .Select(x => eventLog.Entries[x])
                      .Where(x => MatchEventLogEntry(x, options.Type, options.Since))
                      .Take(options.Count)
                      .Each((x, i) => this.AddFile($"EventLogEntry_{i + 1}_{x.EntryType}.txt", FormatContent(x)));
        }

#if NETCOREAPP
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        private static bool MatchEventLogEntry(EventLogEntry entry, EventLogDiagnosticsOptions.EventLogEntryType filter, DateTime? since)
        {
            if (entry.EntryType == 0) // ??
                return false;

            if (!((EventLogEntryType)filter).HasFlag(entry.EntryType))
                return false;

            if (since.HasValue && entry.TimeGenerated < since)
                return false;

            return true;
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

            this.EnsureFileComparisonContent(Path.Combine(path, fileName), content);
            this.EnsureFileComparisonContent(Path.Combine(this.TestDirectory, alternativeFileName), content);
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
                this._testContext.AddResultFile(path);
                return false;
            }

            return true;
        }

        private void RegisterFile(string path, bool scopeIsTestRun)
        {
            if (path.Length > 255)
            {
                throw new ArgumentException(@$"Test result file path too long: {path}
Allowed path length 255: {path.Substring(0, 255)}", nameof(path));
            }

            ICollection<string> files = scopeIsTestRun ? this._testRunFiles : this._testFiles;
            if (files.Contains(path))
                throw new InvalidOperationException($"Test result file already registered: {path}");

            this._testContext.AddResultFile(path);
            files.Add(path);
        }

        private void EnsureWinMergeStarter() => AddTestRunFile("winmerge.bat", $@"@echo off
start winmergeU ""{ExpectedDirectoryName}"" ""{ActualDirectoryName}""");
        private void EnsureTestContextDump() => AddTestRunFile("TestContext.json", JsonConvert.SerializeObject(this._testContext, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new TestContextContractResolver()
        }));

        private void AddTestRunFile(string fileName, string content)
        {
            string path = Path.Combine(this.RunDirectory, fileName);

            if (!this.ShouldRegisterTestRunFile(path))
                return;

            WriteContentToFile(path, content);
            this.RegisterFile(path, scopeIsTestRun: true);
        }

        private static void WriteContentToFile(string path, string content)
        {
            if (File.Exists(path))
                throw new InvalidOperationException($"Path exists already: {path}");

            EnsureDirectory(path);
            File.WriteAllText(path, content);
        }

        private void ZipTestOutput()
        {
            ICollection<string> files = this._testRunFiles.Concat(this._testFiles).ToArray();
            if (!files.Any())
                return;

            string path = Path.Combine(this.TestRootDirectory, $"{this._normalizedTestName}.zip");
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    int relativePathIndex = this.RunDirectory.Length + 1;
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
                string relativeFilePath = file.Substring(this.RunDirectory.Length + 1);
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

        private sealed class TestContextContractResolver : DefaultContractResolver, IContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                JsonProperty result = member.DeclaringType?.FullName switch
                {
                    // Self referencing loop detected for property 'Context' with type 'Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation'. Path ''.
                    "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation" when property.PropertyName == "Context" => null,
                    
                    // We don't need this property for debugging purposes
                    "Microsoft.VisualStudio.TestTools.UnitTesting.TestContext" when property.PropertyName == "CancellationTokenSource" => null,
                    
                    _ => property
                };

                return result;
            }
        }
    }
}