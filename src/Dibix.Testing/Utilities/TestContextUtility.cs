using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestContextUtility
    {
        private static readonly ConcurrentDictionary<string, Lazy<string>> TestRunDirectoryMap = new ConcurrentDictionary<string, Lazy<string>>();
        private static readonly ConcurrentDictionary<TestContext, string> TestNameMap = new ConcurrentDictionary<TestContext, string>();

        public static string GetTestRunDirectory(TestContext testContext)
        {
            // Unfortunately, MSTest does not provide a unique id to identity a test run, therefore we use the generated test run directory from MSTest
            string testRunIdentifier = testContext.TestRunDirectory;
            return TestRunDirectoryMap.GetOrAdd(testRunIdentifier, _ => new Lazy<string>(() => CreateTestRunDirectory(testContext))).Value;
        }

        public static string GetTestName(TestContext testContext)
        {
            if (!DataDrivenTestUtility.IsDataDrivenTest(testContext))
                return testContext.TestName;

            string dataDrivenTestName = TestNameMap.GetOrAdd(testContext, static context => DataDrivenTestUtility.TryGetDataDrivenTestName(context));
            return dataDrivenTestName ?? testContext.TestName;
        }

        private static string CreateTestRunDirectory(TestContext testContext)
        {
            Assembly assembly = TestImplementationResolver.ResolveTestAssembly(testContext);
            string assemblyName = assembly.GetName().Name!;
            string directoryName = $"Run_{DateTime.Now:yyyy-MM-dd HH_mm_ss}";
            string path = Path.Combine(Path.GetTempPath(), "TestResults", directoryName, assemblyName);
            return path;
        }
    }
}