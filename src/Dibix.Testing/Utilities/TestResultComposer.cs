using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private readonly bool _isAssemblyInitialize;
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

        public TestResultComposer(TestContext testContext, bool useDedicatedTestResultsDirectory, bool isAssemblyInitialize)
        {
            _testContext = testContext;
            _useDedicatedTestResultsDirectory = useDedicatedTestResultsDirectory;
            _isAssemblyInitialize = isAssemblyInitialize;
            _defaultRunDirectory = testContext.TestRunResultsDirectory;
            RunDirectory = _useDedicatedTestResultsDirectory ? TestContextUtility.GetTestRunDirectory(testContext) : _defaultRunDirectory;
            TestRootDirectory = Path.Combine(RunDirectory, "Tests");
            _normalizedTestName = String.Join("_", TestContextUtility.GetTestName(testContext).Split(Path.GetInvalidFileNameChars()));
            TestDirectory = Path.Combine(TestRootDirectory, _normalizedTestName);
            _expectedDirectory = Path.Combine(RunDirectory, ExpectedDirectoryName);
            _actualDirectory = Path.Combine(RunDirectory, ActualDirectoryName);
            _testRunFiles = new HashSet<string>();
            _testFiles = new HashSet<string>();
            Initialize();
        }

        public string AddTestFile(string fileName)
        {
            if (_isAssemblyInitialize)
                throw new InvalidOperationException("Test files cannot be added during AssemblyInitialize");

            string path = Path.Combine(TestDirectory, fileName);
            EnsureDirectory(path);
            RegisterFile(path, scopeIsTestRun: false);
            return path;
        }
        public string AddTestFile(string fileName, string content)
        {
            string path = AddTestFile(fileName);
            WriteContentToFile(path, content);
            return path;
        }

        public string ImportTestFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string targetPath = AddTestFile(fileName);
            File.Copy(filePath, targetPath);
            return targetPath;
        }

        public string AddTestRunFile(string fileName) => AddTestRunFile(fileName, out _);
        public string AddTestRunFile(string fileName, string content)
        {
            string path = AddTestRunFile(fileName, out bool result);
            if (result)
                WriteContentToFile(path, content);

            return path;
        }

        public string ImportTestRunFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string targetPath = AddTestRunFile(fileName, out bool fileIsNew);

            if (fileIsNew)
            {
                File.Copy(filePath, targetPath);
            }
            else
            {
                if (new FileInfo(filePath).Length != new FileInfo(targetPath).Length)
                {
                    File.Copy(filePath, targetPath, overwrite: true);
                }
            }

            return targetPath;
        }

        public void AddFileComparison(string expectedContent, string actualContent, string outputName, string extension)
        {
            EnsureFileComparisonContent(_expectedDirectory, outputName, extension, expectedContent);
            EnsureFileComparisonContent(_actualDirectory, outputName, extension, actualContent);
            EnsureWinMergeStarter();
        }

        public void Complete()
        {
            foreach (string resultFile in _testFiles.OrderBy(Path.GetFileName)) 
                _testContext.AddResultFile(resultFile);

            foreach (string resultFile in _testRunFiles.OrderBy(Path.GetFileName)) 
                _testContext.AddResultFile(resultFile);

            if (!_isAssemblyInitialize)
                ZipTestOutput();

            CopyTestOutput();
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

            Func<string, string, string> addTestFileHandler = _isAssemblyInitialize ? AddTestRunFile : AddTestFile;

            addTestFileHandler("EventLogOptions.json", JsonConvert.SerializeObject(options, Formatting.Indented));

            EventLog eventLog = new EventLog(options.LogName, options.MachineName, options.Source);

            Enumerable.Range(0, eventLog.Entries.Count)
                      .Reverse()
                      .Select(x => eventLog.Entries[x])
                      .Where(x => MatchEventLogEntry(x, options.Type, options.Since))
                      .Take(options.Count)
                      .Each((x, i) => addTestFileHandler($"EventLogEntry_{i + 1}_{x.EntryType}.txt", FormatContent(x)));
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

        private void Initialize()
        {
            DirectoryInfo runDirectory = new DirectoryInfo(RunDirectory);
            if (runDirectory.Exists)
            {
                // Import previous run attachments possibly from AssemblyInitialize
                foreach (FileInfo existingRunFile in runDirectory.EnumerateFiles())
                {
                    string filePath = existingRunFile.FullName;
                    _testRunFiles.Add(filePath);
                }
            }

            EnsureTestContextDump();
            EnsureEnvironmentDump();
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
            EnsureFileComparisonContent(Path.Combine(TestDirectory, alternativeFileName), content);
        }
        private void EnsureFileComparisonContent(string path, string content)
        {
            WriteContentToFile(path, content);
            RegisterFile(path, scopeIsTestRun: false);
        }

        private bool ShouldRegisterTestRunFile(string path)
        {
            if (File.Exists(path))
            {
                // Ultimately, test run files are collected once the whole test run is completed.
                // Unfortunately, there is no easy way for us to execute code at this level apart from AssemblyCleanup.
                // However the AssemblyCleanup method doesn't accept a TestContext and is also not inheritable.
                // Therefore this step is executed after each test and involves to skip these files in subsequent tests of the current run.
                _testRunFiles.Add(path);
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

            ICollection<string> files = scopeIsTestRun ? _testRunFiles : _testFiles;
            if (files.Contains(path))
                throw new InvalidOperationException($"Test result file already registered: {path}");

            files.Add(path);
        }

        private void EnsureWinMergeStarter() => AddTestRunFile("winmerge.bat", $@"@echo off
start winmergeU ""{ExpectedDirectoryName}"" ""{ActualDirectoryName}""");

        private void EnsureTestContextDump() => AddTestRunFile("TestContext.json", JsonConvert.SerializeObject(_testContext, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new TestContextContractResolver()
        }));

        private void EnsureEnvironmentDump()
        {
            IDictionary<string, object> environmentVariables = Environment.GetEnvironmentVariables()
                                                                          .Cast<DictionaryEntry>()
                                                                          .ToDictionary(x => (string)x.Key, x => x.Value);
            int maxKeyLength = environmentVariables.Keys.Max(x => x.Length);

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> environmentVariable in environmentVariables.OrderBy(x => x.Key))
            {
                sb.Append(environmentVariable.Key.PadRight(maxKeyLength));
                sb.Append(" = ");
                sb.Append(environmentVariable.Value);
                sb.AppendLine();
            }

            string environment = sb.ToString();
            AddTestRunFile("Environment.txt", environment);
        }

        private string AddTestRunFile(string fileName, out bool result)
        {
            string path = Path.Combine(RunDirectory, fileName);

            result = ShouldRegisterTestRunFile(path);
            if (!result)
                return path;

            RegisterFile(path, scopeIsTestRun: true);
            return path;
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
            ICollection<string> files = _testRunFiles.Concat(_testFiles).ToArray();
            if (!files.Any())
                return;

            string path = Path.Combine(TestRootDirectory, $"{_normalizedTestName}.zip");
            EnsureDirectory(path);
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    int relativePathIndex = RunDirectory.Length + 1;
                    string relativePath = file.Substring(relativePathIndex, file.Length - relativePathIndex);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            }

            RegisterFile(path, scopeIsTestRun: false);
        }

        private void CopyTestOutput()
        {
            if (!_useDedicatedTestResultsDirectory)
                return;

            CopyFiles(_testRunFiles, _defaultRunDirectory, ignoreIfExists: true);
            CopyFiles(_testFiles, _defaultRunDirectory);
        }

        private void CopyFiles(IEnumerable<string> files, string targetDirectory, bool ignoreIfExists = false)
        {
            foreach (string file in files)
            {
                string relativeFilePath = file.Substring(RunDirectory.Length + 1);
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