using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DefaultAssemblyResolver : ReferencedAssemblyInspector
    {
        private readonly ArtifactGenerationConfiguration _configuration;
        private readonly IDictionary<string, string> _assemblyReferenceMap;

        public DefaultAssemblyResolver(SqlCoreConfiguration globalConfiguration, ArtifactGenerationConfiguration artifactGenerationConfiguration)
        {
            _configuration = artifactGenerationConfiguration;
            _assemblyReferenceMap = artifactGenerationConfiguration.References
                                                                   .Select(x => x.GetFullPath())
                                                                   .ToDictionary(Path.GetFileNameWithoutExtension, x => Path.GetFullPath(Path.Combine(globalConfiguration.ProjectDirectory, x)));
        }

        protected override IEnumerable<string> GetReferencedAssemblies() => _assemblyReferenceMap.Values;

        protected override bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            if (_assemblyReferenceMap.TryGetValue(assemblyName, out path))
                return true;

            if (String.IsNullOrEmpty(_configuration.ExternalAssemblyReferenceDir))
                return false;

            string externalAssemblyReferencePath = Path.Combine(_configuration.ExternalAssemblyReferenceDir, $"{assemblyName}.dll");
            if (File.Exists(externalAssemblyReferencePath))
            {
                path = externalAssemblyReferencePath;
                return true;
            }

            return false;
        }
    }
}