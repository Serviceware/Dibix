using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyResolver : ReferencedAssemblyInspector
    {
        private readonly string _externalAssemblyReferenceDirectory;
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public DefaultAssemblyResolver(string projectDirectory, string externalAssemblyReferenceDirectory, IEnumerable<string> assemblyReferences)
        {
            this._externalAssemblyReferenceDirectory = externalAssemblyReferenceDirectory;
            this._assemblyReferenceMap = assemblyReferences.ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(projectDirectory, x)));
        }

        protected override IEnumerable<string> GetReferencedAssemblies() => this._assemblyReferenceMap.Values;

        protected override bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            if (this._assemblyReferenceMap.TryGetValue(assemblyName, out path))
                return true;

            if (String.IsNullOrEmpty(this._externalAssemblyReferenceDirectory))
                return false;

            string externalAssemblyReferencePath = Path.Combine(this._externalAssemblyReferenceDirectory, $"{assemblyName}.dll");
            if (File.Exists(externalAssemblyReferencePath))
            {
                path = externalAssemblyReferencePath;
                return true;
            }

            return false;
        }
    }
}