using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Dibix.Testing
{
    internal sealed class TestResultFileManager
    {
        private readonly TestRunTestResultFileComposer _testRunFileComposer;
        private readonly TestMethodTestResultFileComposer _testMethodFileComposer;
        private readonly WinMergeComparisonComposer _winMergeComparisonComposer;
        private readonly TestContext _testContext;
        private readonly TestClassInstanceScope _scope;
        private bool _eventLogCollected;

        public string RunDirectory => _testRunFileComposer.ResultDirectory;
        public string TestDirectory => _testMethodFileComposer.ResultDirectory;

        private TestResultFileManager(TestRunTestResultFileComposer testRunFileComposer, TestMethodTestResultFileComposer testMethodFileComposer, WinMergeComparisonComposer winMergeComparisonComposer, TestContext testContext, TestClassInstanceScope scope)
        {
            _testRunFileComposer = testRunFileComposer;
            _testMethodFileComposer = testMethodFileComposer;
            _winMergeComparisonComposer = winMergeComparisonComposer;
            _testContext = testContext;
            _scope = scope;
        }

        public static TestResultFileManager FromTestContext(TestContext testContext, bool useDedicatedTestResultsDirectory, TestClassInstanceScope scope)
        {
            // Unfortunately, MSTest does not provide a method that is called, whenever a test run starts.
            // The closest would be AssemblyInitialize, but only the assemblies where the tests are executed, are scanned, which kind of makes sense.
            // So now we are using TestInitialize which works on base classes and is executed for each test.
            // To ensure the test run context is only initialized once, we are using a cache based on the test run directory.
            TestRunTestResultFileComposer testRunFileComposer = TestRunTestResultFileComposer.Resolve(testContext, useDedicatedTestResultsDirectory);
            string resultTestsDirectory = Path.Combine(testRunFileComposer.ResultDirectory, "Tests");
            TestMethodTestResultFileComposer testMethodFileComposer = TestMethodTestResultFileComposer.Create(testContext, resultTestsDirectory);
            WinMergeComparisonComposer winMergeComparisonComposer = new WinMergeComparisonComposer(testMethodFileComposer, testRunFileComposer, testContext);
            TestResultFileManager resultFileComposer = new TestResultFileManager(testRunFileComposer, testMethodFileComposer, winMergeComparisonComposer, testContext, scope);
            resultFileComposer.Initialize();
            return resultFileComposer;
        }

        public string AddTestFile(string fileName)
        {
            if (_scope == TestClassInstanceScope.AssemblyInitialize)
                throw new InvalidOperationException("Test files cannot be added during AssemblyInitialize");

            string path = _testMethodFileComposer.AddResultFile(fileName);
            return path;
        }
        public string AddTestFile(string fileName, string content) => _testMethodFileComposer.AddResultFile(fileName, content);

        public string ImportTestFile(string filePath) => _testMethodFileComposer.ImportResultFile(filePath);

        public string AddTestRunFile(string fileName) => _testRunFileComposer.AddResultFile(fileName, _testContext);
        public string AddTestRunFile(string fileName, string content) => _testRunFileComposer.AddResultFile(fileName, content, _testContext);

        public string ImportTestRunFile(string filePath) => _testRunFileComposer.ImportResultFile(filePath, _testContext);

        public void AddFileComparison(string expectedContent, string actualContent, string outputName, string extension) => _winMergeComparisonComposer.AddFileComparison(expectedContent, actualContent, outputName, extension);

        public void Complete()
        {
            if (_scope == TestClassInstanceScope.AssemblyInitialize)
                return;

            ICollection<string> resultFiles = new TestResultFileComposer[] { _testRunFileComposer, _testMethodFileComposer }.SelectMany(x => x.ResultFiles).Where(File.Exists).ToArray();
            if (!resultFiles.Any())
                return;

            _testMethodFileComposer.ZipTestOutput(_testRunFileComposer.ResultDirectory, resultFiles);

            // Unfortunately, MSTest does not provide a method that is called, whenever a test run ends.
            // The closest would be AssemblyCleanup, but only the assemblies where the tests are executed, are scanned, which kind of makes sense.
            // So now we are using Dispose which works on base classes and is executed for each test.
            // To ensure the test run output is only collected once, we have to take note of files already being deployed.
            _testRunFileComposer.CopyTestOutput(resultFiles);
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

            TestResultFileComposer composer = GetTargetTestResultFileComposer(_scope);

            composer.AddResultFile("EventLogOptions.json", JsonConvert.SerializeObject(options, Formatting.Indented), _testContext);

            EventLog eventLog = new EventLog(options.LogName, options.MachineName, options.Source);

            Enumerable.Range(0, eventLog.Entries.Count)
                      .Reverse()
                      .Select(x => eventLog.Entries[x])
                      .Where(x => MatchEventLogEntry(x, options.Type, options.Since))
                      .Take(options.Count)
                      .Each((x, i) => composer.AddResultFile($"EventLogEntry_{i + 1}_{x.EntryType}.txt", FormatContent(x), _testContext));
        }

        private void Initialize()
        {
            if (_scope == TestClassInstanceScope.AssemblyInitialize)
                return;

            _testMethodFileComposer.RegisterTextContextInfo(_testContext);
        }

        private TestResultFileComposer GetTargetTestResultFileComposer(TestClassInstanceScope scope) => scope switch
        {
            TestClassInstanceScope.AssemblyInitialize => _testRunFileComposer,
            TestClassInstanceScope.TestInitialize => _testMethodFileComposer,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };

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
    }
}