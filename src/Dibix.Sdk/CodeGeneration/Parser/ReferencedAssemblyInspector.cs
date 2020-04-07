using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ReferencedAssemblyInspector : AssemblyResolver
    {
        public T Inspect<T>(Func<IEnumerable<Assembly>, T> referencedAssembliesHandler)
        {
            try
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
                T result = referencedAssembliesHandler(this.GetReferencedAssemblies().Select(base.LoadAssembly));
                return result;
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
            }
        }

        protected abstract IEnumerable<string> GetReferencedAssemblies();

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