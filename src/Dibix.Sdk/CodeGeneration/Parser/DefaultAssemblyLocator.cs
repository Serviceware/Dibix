using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyLocator : IAssemblyLocator
    {
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public DefaultAssemblyLocator(string projectDirectory, IEnumerable<string> assemblyReferences)
        {
            this._assemblyReferenceMap = assemblyReferences.ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(projectDirectory, x)));
        }

        public bool TryGetAssemblyLocation(string assemblyName, out string path) => this._assemblyReferenceMap.TryGetValue(assemblyName, out path);
    }
}