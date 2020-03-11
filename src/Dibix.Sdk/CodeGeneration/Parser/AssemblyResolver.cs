using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class AssemblyResolver
    {
        // Since we are not loading the assembly the traditional way using a name or location, the framework probably cannot cache it.
        // So when the same assembly is loaded twice, we get an exception.
        // Therefore we have to statically cache it, to make it available to the entire VS instance across multiple dibix build tasks.
        private static readonly IDictionary<string, Assembly> AssemblyNameCache = new Dictionary<string, Assembly>();
        private static readonly IDictionary<string, Assembly> AssemblyLocationCache = new Dictionary<string, Assembly>();

        public bool TryGetAssembly(string assemblyName, out Assembly assembly)
        {
            if (AssemblyNameCache.TryGetValue(assemblyName, out assembly))
                return true;

            if (!this.TryGetAssemblyLocation(assemblyName, out string assemblyLocation))
                return false;

            assembly = this.LoadAssembly(assemblyLocation);
            AssemblyNameCache.Add(assemblyName, assembly);
            return true;
        }

        protected Assembly LoadAssembly(string assemblyLocation)
        {
            if (AssemblyLocationCache.TryGetValue(assemblyLocation, out Assembly assembly))
                return assembly;

            // Both these approaches keep the file locked in visual studio..
            // 1. Assembly.LoadFrom(assemblyPath);
            // 2. Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            // see: https://www.codeproject.com/Tips/836907/Loading-Assembly-to-Leave-Assembly-File-Unlocked
            assembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(assemblyLocation));

            AssemblyLocationCache.Add(assemblyLocation, assembly);
            return assembly;
        }

        protected abstract bool TryGetAssemblyLocation(string assemblyName, out string path);
    }
}