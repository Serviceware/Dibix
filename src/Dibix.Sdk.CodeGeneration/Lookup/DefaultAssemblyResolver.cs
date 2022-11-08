using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyResolver : ReferencedAssemblyInspector
    {
        private readonly string _externalAssemblyReferenceDir;
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public DefaultAssemblyResolver(string projectDirectory, string externalAssemblyReferenceDirectory, IEnumerable<TaskItem> references)
        {
            _externalAssemblyReferenceDir = externalAssemblyReferenceDirectory;
            _assemblyReferenceMap = references.Select(x => x.GetFullPath())
                                              .ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(projectDirectory, x)));
        }

        protected override IEnumerable<string> GetReferencedAssemblies() => _assemblyReferenceMap.Values;

        protected override bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            if (_assemblyReferenceMap.TryGetValue(assemblyName, out path))
                return true;

            if (String.IsNullOrEmpty(_externalAssemblyReferenceDir))
                return false;

            string externalAssemblyReferencePath = Path.Combine(_externalAssemblyReferenceDir, $"{assemblyName}.dll");
            if (File.Exists(externalAssemblyReferencePath))
            {
                path = externalAssemblyReferencePath;
                return true;
            }

            return false;
        }
    }
}