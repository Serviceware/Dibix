using System;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public static class ReflectionOnlyTypeInspector
    {
        public static T Inspect<T>(Func<T> handler, AssemblyResolver assemblyResolver)
        {
            // Cannot resolve dependency to assembly '' because it has not been preloaded.
            // When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.
            Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);

                if (assemblyResolver.TryGetAssembly(assemblyName.Name, out Assembly matchingAssembly))
                    return matchingAssembly;

                return Assembly.ReflectionOnlyLoad(args.Name);
            }

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
    }
}