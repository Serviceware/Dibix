using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyResolver : ReferencedAssemblyInspector
    {
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public DefaultAssemblyResolver(string projectDirectory, IEnumerable<string> assemblyReferences)
        {
            this._assemblyReferenceMap = assemblyReferences.ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(projectDirectory, x)));
        }

        protected override IEnumerable<string> GetReferencedAssemblies() => this._assemblyReferenceMap.Values;

        protected override bool TryGetAssemblyLocation(string assemblyName, out string path) => this._assemblyReferenceMap.TryGetValue(assemblyName, out path);

    }
}