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
            string privateResultsDirectory = (string)testContext.Properties["PrivateTestResultsDirectory"];
            isSpecified = !String.IsNullOrEmpty(privateResultsDirectory);
            if (!isSpecified)
                privateResultsDirectory = testContext.TestRunResultsDirectory;

            Directory.CreateDirectory(privateResultsDirectory);
            return privateResultsDirectory;
        }

        public static string GetPublicResultsDirectory(this TestContext context, string privateResultsDirectory)
        {
            string publicResultsDirectory = context.Properties["PublicTestResultsDirectory"] as string;
            if (String.IsNullOrEmpty(publicResultsDirectory))
                publicResultsDirectory = privateResultsDirectory;

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

        [SupportedOSPlatform("windows")]
        public static void AddLastEventLogErrors(this TestContext context, int count = 5)
        {
            string privateResultsDirectory = context.GetPrivateResultsDirectory(out _);
            EventLog eventLog = new EventLog("Application");

            eventLog.Entries
                    .Cast<EventLogEntry>()
                    .Where(x => x.EntryType == EventLogEntryType.Error)
                    .OrderByDescending(x => x.TimeWritten)
                    .Take(count)
                    .Each((x, i) => AddResultFile(context, privateResultsDirectory, $"EventLogError_{i}.txt", $@"{DateTime.Now:O}
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
    }
}