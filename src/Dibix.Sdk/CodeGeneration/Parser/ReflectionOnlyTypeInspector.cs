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
            Assembly assembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (assembly != null)
                return assembly;

            assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (assembly != null)
                return Assembly.ReflectionOnlyLoadFrom(assembly.Location);

            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}