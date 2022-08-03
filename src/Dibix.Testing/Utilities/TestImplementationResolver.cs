using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestImplementationResolver
    {
        public static Assembly ResolveTestAssembly(TestContext testContext)
        {
            Assembly assembly = TryResolveTestAssemblyFromTestContext(testContext);

            if (assembly == null)
                assembly = TryResolveTestAssemblyFromCallStack();

            if (assembly == null)
                assembly = TryResolveTestAssemblyFromTestContextImplementation(testContext);

            if (assembly == null)
                throw new InvalidOperationException("Could not determine test assembly");

            return assembly;
        }

        public static MethodInfo ResolveTestMethod(TestContext testContext) => ResolveTestMethodFromTestContext(testContext);

        // Not stable, because it uses reflection and expects a specific test host implementation
        private static Assembly TryResolveTestAssemblyFromTestContextImplementation(TestContext testContext)
        {
            Type testContextImplementationType = testContext.GetType();
            FieldInfo testMethodField = testContextImplementationType.GetField("testMethod", BindingFlags.NonPublic | BindingFlags.Instance);
            if (testMethodField == null) 
                return null;
            
            Type testMethodType = testMethodField.FieldType;
            PropertyInfo assemblyNameProperty = testMethodType.GetProperty("AssemblyName");
            if (assemblyNameProperty == null) 
                return null;

            object testContextImplementation = testMethodField.GetValue(testContext);
            string assemblyName = assemblyNameProperty.GetValue(testContextImplementation) as string;
            Assembly assembly = AppDomain.CurrentDomain
                                         .GetAssemblies()
                                         .FirstOrDefault(x => !x.IsDynamic && x.Location == assemblyName);
            return assembly;

        }

        // Not stable when async call stack is involved
        private static Assembly TryResolveTestAssemblyFromCallStack()
        {
            Assembly assembly = new StackTrace().GetFrames()
                                                .Select(x => x.GetMethod())
                                                .Where(x => (x?.IsDefined(typeof(TestMethodAttribute))).GetValueOrDefault(false))
                                                .Select(x => x.DeclaringType?.Assembly)
                                                .FirstOrDefault();

            return assembly;
        }

        private static Assembly TryResolveTestAssemblyFromTestContext(TestContext testContext)
        {
            Assembly assembly = AppDomain.CurrentDomain
                                         .GetAssemblies()
                                         .Select(x => x.GetType(testContext.FullyQualifiedTestClassName))
                                         .Where(x => x != null)
                                         .Select(x => x.Assembly)
                                         .FirstOrDefault();
            return assembly;
        }

        private static MethodInfo ResolveTestMethodFromTestContext(TestContext testContext)
        {
            string testClassName = testContext.FullyQualifiedTestClassName;
            Type testClass = AppDomain.CurrentDomain
                                      .GetAssemblies()
                                      .Select(x => x.GetType(testClassName))
                                      .FirstOrDefault(x => x != null);

            if (testClass == null)
                throw new InvalidOperationException($"Could not resolve test class: {testClassName}");

            string testMethodName = testContext.TestName;
            MethodInfo testMethod = testClass.SafeGetMethod(testMethodName);
            return testMethod;
        }
    }
}