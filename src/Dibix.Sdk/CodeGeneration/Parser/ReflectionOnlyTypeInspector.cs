﻿using System;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public static class ReflectionOnlyTypeInspector
    {
        public static T Inspect<T>(Func<T> handler)
        {
            try
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
                return handler();
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
            }
        }

        // Cannot resolve dependency to assembly '' because it has not been preloaded.
        // When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.
        private static Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            bool IsMatchingAssembly(Assembly assembly) => assembly.GetName().Name == assemblyName.Name;

            Assembly matchingAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(IsMatchingAssembly);
            if (matchingAssembly != null)
                return matchingAssembly;

            matchingAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(IsMatchingAssembly);
            if (matchingAssembly != null)
                return Assembly.ReflectionOnlyLoadFrom(matchingAssembly.Location);

            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}