using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyResolver : AssemblyResolver, IReferencedAssemblyProvider
    {
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public IEnumerable<Assembly> ReferencedAssemblies => this._assemblyReferenceMap.Values.Select(base.LoadAssembly);

        public DefaultAssemblyResolver(string projectDirectory, IEnumerable<string> assemblyReferences)
        {
            this._assemblyReferenceMap = assemblyReferences.ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(projectDirectory, x)));
        }

        protected override bool TryGetAssemblyLocation(string assemblyName, out string path) => this._assemblyReferenceMap.TryGetValue(assemblyName, out path);

    }
}