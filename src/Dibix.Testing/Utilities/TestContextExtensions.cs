using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestContextExtensions
    {
        public static string GetPrivateResultsDirectory(this TestContext testContext, out bool isSpecified)
        {
            string privateResultsDirectory = ResolvePrivateResultsDirectory(testContext, out isSpecified);
            string targetDirectory = EnsureIsolatedTestResultsDirectory(testContext, privateResultsDirectory);
            return targetDirectory;
        }
        private static string ResolvePrivateResultsDirectory(TestContext testContext, out bool isSpecified)
        {
            string privateResultsDirectory = (string)testContext.Properties["PrivateTestResultsDirectory"];
            isSpecified = !String.IsNullOrEmpty(privateResultsDirectory);
            if (!isSpecified)
                privateResultsDirectory = testContext.TestRunResultsDirectory;

            return privateResultsDirectory;
        }

        public static string GetPublicResultsDirectory(this TestContext testContext)
        {
            string publicResultsDirectory = ResolvePublicResultsDirectory(testContext);
            string targetDirectory = EnsureIsolatedTestResultsDirectory(testContext, publicResultsDirectory);
            return targetDirectory;
        }
        private static string ResolvePublicResultsDirectory(TestContext testContext)
        {
            string publicResultsDirectory = testContext.Properties["PublicTestResultsDirectory"] as string;
            if (String.IsNullOrEmpty(publicResultsDirectory))
                publicResultsDirectory = ResolvePrivateResultsDirectory(testContext, out _);

            return publicResultsDirectory;
        }

        public static void AddDiffToolInvoker(this TestContext testContext, string expected, string actual)
        {
            string batchPath = DiffToolUtility.GenerateReferencingBatchFile(testContext, expected, actual, out bool privateResultsDirectorySpecified);
            testContext.AddResultFile(batchPath);

            if (!privateResultsDirectorySpecified) // TODO: Need to check if test is ran locally or from Azure DevOps
            {
                Process.Start(batchPath);
            }
        }

#if NET5_0
        [SupportedOSPlatform("windows")]
#endif
        public static void AddLastEventLogErrors(this TestContext context, int count = 5) => AddLastEventLogEntries(context, EventLogEntryType.Error, count);
#if NET5_0
        [SupportedOSPlatform("windows")]
#endif
        private static void AddLastEventLogEntries(this TestContext context, EventLogEntryType eventLogEntryType, int count)
        {
            string privateResultsDirectory = context.GetPrivateResultsDirectory(out _);
            EventLog eventLog = new EventLog("Application");

            eventLog.Entries
                    .Cast<EventLogEntry>()
                    .Where(x => x.EntryType == eventLogEntryType)
                    .OrderByDescending(x => x.TimeWritten)
                    .Take(count)
                    .Each((x, i) => AddResultFile(context, privateResultsDirectory, $"EventLog{eventLogEntryType}_{i}.txt", $@"{DateTime.Now:O}
---
{x.Message}"));
        }

        public static void AddResultFile(this TestContext context, string fileName, string content)
        {
            string privateResultsDirectory = context.GetPrivateResultsDirectory(out _);
            AddResultFile(context, privateResultsDirectory, fileName, content);
        }
        private static void AddResultFile(this TestContext context, string directory, string fileName, string content)
        {
            string filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
            context.AddResultFile(filePath);
        }

        private static string EnsureIsolatedTestResultsDirectory(TestContext testContext, string directory)
        {
            string targetDirectory = Path.Combine(directory, testContext.TestName);
            Directory.CreateDirectory(targetDirectory);
            return targetDirectory;
        }
    }
}