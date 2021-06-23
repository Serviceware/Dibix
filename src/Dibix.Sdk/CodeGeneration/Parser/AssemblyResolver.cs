using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class AssemblyResolver
    {
        public bool TryGetAssembly(string assemblyName, out Assembly assembly)
        {
            if (TryGetLoadedAssembly(assemblyName, out assembly))
                return true;

            if (!this.TryGetAssemblyLocation(assemblyName, out string assemblyLocation))
                return false;

            assembly = LoadAssemblyCore(assemblyLocation);
            return true;
        }

        public static bool TryGetLoadedAssembly(string assemblyName, out Assembly assembly)
        {
            bool IsMatchingAssembly(Assembly candidate) => candidate.GetName().Name == assemblyName;

            Assembly matchingAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(IsMatchingAssembly);
            if (matchingAssembly != null)
            {
                assembly = matchingAssembly;
                return true;
            }

            matchingAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(IsMatchingAssembly);
            if (matchingAssembly != null)
            {
                assembly = Assembly.ReflectionOnlyLoadFrom(matchingAssembly.Location);
                return true;
            }

            assembly = null;
            return false;
        }

        protected Assembly LoadAssembly(string assemblyLocation)
        {
            if (TryGetLoadedAssembly(Path.GetFileNameWithoutExtension(assemblyLocation), out Assembly assembly))
                return assembly;

            return LoadAssemblyCore(assemblyLocation);
        }

        protected abstract bool TryGetAssemblyLocation(string assemblyName, out string path);

        private static Assembly LoadAssemblyCore(string assemblyLocation)
        {
            // Both these approaches keep the file locked in visual studio..
            // 1. Assembly.LoadFrom(assemblyPath);
            // 2. Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            // see: https://www.codeproject.com/Tips/836907/Loading-Assembly-to-Leave-Assembly-File-Unlocked
            return Assembly.ReflectionOnlyLoad(File.ReadAllBytes(assemblyLocation));
        }
    }
}