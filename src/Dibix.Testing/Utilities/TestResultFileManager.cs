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
            TestRunTestResultFileComposer testRunFileComposer = TestRunTestResultFileComposer.Resolve(testContext, useDedicatedTestResultsDirectory);
            string resultTestsDirectory = Path.Combine(testRunFileComposer.ResultDirectory, "Tests");
            TestMethodTestResultFileComposer testMethodFileComposer = TestMethodTestResultFileComposer.Create(testContext, resultTestsDirectory);
            WinMergeComparisonComposer winMergeComparisonComposer = new WinMergeComparisonComposer(testMethodFileComposer, testRunFileComposer, testContext);
            TestResultFileManager resultFileComposer = new TestResultFileManager(testRunFileComposer, testMethodFileComposer, winMergeComparisonComposer, testContext, scope);
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

            ICollection<string> resultFiles = new TestResultFileComposer[] { _testRunFileComposer, _testMethodFileComposer }.SelectMany(x => x.ResultFiles).ToArray();
            if (!resultFiles.Any())
                return;

            _testMethodFileComposer.ZipTestOutput(_testRunFileComposer.ResultDirectory, resultFiles);
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