using System;
using System.Diagnostics;
using System.IO;
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

        public static void AddDiffToolInvoker(this TestContext testContext, string expected, string actual, string extension)
        {
            string batchPath = DiffToolUtility.GenerateReferencingBatchFile(testContext, expected, actual, extension, out bool privateResultsDirectorySpecified);
            testContext.AddResultFile(batchPath);

            if (!privateResultsDirectorySpecified) // TODO: Need to check if test is ran locally or from Azure DevOps
            {
                Process.Start(batchPath);
            }
        }

        private static string EnsureIsolatedTestResultsDirectory(TestContext testContext, string directory)
        {
            string targetDirectory = Path.Combine(directory, testContext.TestName);
            Directory.CreateDirectory(targetDirectory);
            return targetDirectory;
        }
    }
}