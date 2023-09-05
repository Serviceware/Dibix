using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class DataDrivenTestUtility
    {
        public static bool IsDataDrivenTest(TestContext testContext)
        {
            MethodInfo testMethod = TestImplementationResolver.ResolveTestMethod(testContext);
            bool hasDataRowAttribute = testMethod.IsDefined(typeof(DataRowAttribute));
            bool hasDynamicDataAttribute = testMethod.IsDefined(typeof(DynamicDataAttribute));
            return hasDataRowAttribute || hasDynamicDataAttribute;
        }

        public static string TryGetDataDrivenTestName(TestContext testContext)
        {
            const string testContextImplementationTypeName = "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation,Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices";
            Type expectedTestContextImplementationType = Type.GetType(testContextImplementationTypeName, throwOnError: true);

            Type actualTestContextImplementationType = testContext.GetType();
            if (actualTestContextImplementationType != expectedTestContextImplementationType)
            {
                throw new InvalidOperationException($@"Unexpected test context implementation type: {actualTestContextImplementationType}
Expected: {expectedTestContextImplementationType}");
            }

            const string testMethodFieldName = "_testMethod";
            FieldInfo testMethodFieldInfo = expectedTestContextImplementationType.GetField(testMethodFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (testMethodFieldInfo == null)
            {
                throw new InvalidOperationException($"Could not find private field '{testMethodFieldName}' on type {expectedTestContextImplementationType.FullName}");
            }

            const string testMethodImplementationTypeName = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel.TestMethod,Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter";
            Type expectedTestMethodImplementationType = Type.GetType(testMethodImplementationTypeName, throwOnError: true);

            object testMethod = testMethodFieldInfo.GetValue(testContext);
            Type actualTestMethodImplementationType = testMethod.GetType();
            if (actualTestMethodImplementationType != expectedTestMethodImplementationType)
            {
                throw new InvalidOperationException($@"Unexpected test method implementation type: {actualTestMethodImplementationType}
Expected: {expectedTestMethodImplementationType}");
            }

            const string testMethodDisplayNamePropertyName = "DisplayName";
            PropertyInfo displayNamePropertyInfo = expectedTestMethodImplementationType.GetProperty(testMethodDisplayNamePropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (displayNamePropertyInfo == null)
            {
                throw new InvalidOperationException($"Could not find internal property '{testMethodDisplayNamePropertyName}' on type {expectedTestMethodImplementationType.FullName}");
            }

            string displayName = (string)displayNamePropertyInfo.GetValue(testMethod);
            return displayName;
        }
    }
}