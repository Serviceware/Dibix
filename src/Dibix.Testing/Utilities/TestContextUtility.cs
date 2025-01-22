using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestContextUtility
    {
        private static readonly ConcurrentDictionary<TestContext, string> TestNameMap = new ConcurrentDictionary<TestContext, string>();

        public static string GetTestName(TestContext testContext)
        {
            if (!DataDrivenTestUtility.IsDataDrivenTest(testContext))
                return testContext.TestName;

            string dataDrivenTestName = TestNameMap.GetOrAdd(testContext, static context => DataDrivenTestUtility.GetDataDrivenTestName(context));
            return dataDrivenTestName ?? testContext.TestName;
        }
    }
}