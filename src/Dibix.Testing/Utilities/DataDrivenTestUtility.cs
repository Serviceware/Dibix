using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class DataDrivenTestUtility
    {
        private static readonly Func<TestContext, string> GetTestMethodDisplayName = CompileGetTestMethodDisplayName();

        public static bool IsDataDrivenTest(TestContext testContext)
        {
            MethodInfo testMethod = TestImplementationResolver.ResolveTestMethod(testContext);
            bool hasDataRowAttribute = testMethod.IsDefined(typeof(DataRowAttribute));
            bool hasDynamicDataAttribute = testMethod.IsDefined(typeof(DynamicDataAttribute));
            return hasDataRowAttribute || hasDynamicDataAttribute;
        }

        public static string GetDataDrivenTestName(TestContext testContext)
        {
            string displayName = GetTestMethodDisplayName(testContext);
            return displayName;
        }

        private static Func<TestContext, string> CompileGetTestMethodDisplayName()
        {
            ParameterExpression testContextParameter = Expression.Parameter(typeof(TestContext), "testContext");

            const string testContextImplementationTypeName = "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.TestContextImplementation,Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices";
            const string testMethodImplementationTypeName = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel.TestMethod,Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter";
            Type testContextImplementationType = Type.GetType(testContextImplementationTypeName, throwOnError: true);
            Type expectedTestMethodImplementationType = Type.GetType(testMethodImplementationTypeName, throwOnError: true);

            Expression testContextImplementation = Expression.Convert(testContextParameter, testContextImplementationType);
            Expression testMethodField = Expression.Field(testContextImplementation, "_testMethod");
            Expression testMethodImplementation = Expression.Convert(testMethodField, expectedTestMethodImplementationType);
            Expression displayName = Expression.Property(testMethodImplementation, "DisplayName");

            Expression<Func<TestContext, string>> lambda = Expression.Lambda<Func<TestContext, string>>(displayName, testContextParameter);
            Func<TestContext, string> compiled = lambda.Compile();

            return compiled;
        }
    }
}