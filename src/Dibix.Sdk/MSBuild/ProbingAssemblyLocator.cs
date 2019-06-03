using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class ProbingAssemblyLocator : IAssemblyLocator
    {
        private readonly ICollection<string> _probingDirectories;

        public ProbingAssemblyLocator(ICollection<string> probingDirectories)
        {
            this._probingDirectories = probingDirectories;
        }

        public bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            path = this._probingDirectories
                       .Select(x => Path.Combine(x, $"{assemblyName}.dll"))
                       .FirstOrDefault(File.Exists);

            return path != null;
        }
    }
}