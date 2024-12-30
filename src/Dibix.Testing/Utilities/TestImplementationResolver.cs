using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public static class TestImplementationResolver
    {
        private static readonly ConcurrentDictionary<TestContext, Assembly> TestAssemblyCache = new ConcurrentDictionary<TestContext, Assembly>();

        public static Assembly ResolveTestAssembly(TestContext testContext) => TestAssemblyCache.GetOrAdd(testContext, ResolveTestAssemblyCore);

        public static MethodInfo ResolveTestMethod(TestContext testContext) => ResolveTestMethodFromTestContext(testContext);

        private static Assembly ResolveTestAssemblyCore(TestContext testContext)
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
            
            if (testContext.ManagedMethod != null)
            {
                int startIndex = testContext.ManagedMethod.IndexOf('(');
                if (startIndex > 0)
                {
                    // Data driven test
                    int endIndex = testContext.ManagedMethod.IndexOf(')', startIndex);
                    string parameters = testContext.ManagedMethod.Substring(startIndex + 1, endIndex - startIndex - 1);
                    string[] parameterTypeNames = parameters.Split(',');

                    // We can't use Type.GetType here because the parameter type names are not assembly qualified
                    //types = parameterTypeNames.Select(x => Type.GetType(x, throwOnError: true)).ToArray();
                    MethodInfo dataDrivenTestMethod = ResolveDataDrivenTestMethod(testClass, testMethodName, parameterTypeNames);
                    return dataDrivenTestMethod;
                }
            }

            MethodInfo testMethod = testClass.SafeGetMethod(testMethodName);
            return testMethod;
        }

        private static MethodInfo ResolveDataDrivenTestMethod(Type type, string methodName, string[] parameterTypeNames)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            MethodInfo method = ResolveDataDrivenTestMethod(type, methodName, parameterTypeNames, bindingFlags);
            if (method == null)
                throw new InvalidOperationException($"Could not find method {type}.{methodName}({String.Join(", ", parameterTypeNames)}) [{bindingFlags}]");

            return method;
        }
        private static MethodInfo ResolveDataDrivenTestMethod(IReflect type, string methodName, IReadOnlyList<string> parameterTypeNames, BindingFlags bindingFlags)
        {
            foreach (MethodInfo methodInfo in type.GetMethods(bindingFlags))
            {
                if (methodInfo.Name != methodName)
                    continue;

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length != parameterTypeNames.Count)
                    continue;

                bool parametersMatch = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    string expectedParameterTypeName = parameterTypeNames[i];
                    string actualParameterTypeName = parameters[i].ParameterType.FullName;
                    if (expectedParameterTypeName == actualParameterTypeName)
                        continue;

                    parametersMatch = false;
                    break;
                }

                if (!parametersMatch)
                    continue;

                return methodInfo;
            }

            return null;
        }
    }
}