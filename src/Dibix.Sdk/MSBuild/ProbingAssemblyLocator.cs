using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class ProbingAssemblyLocator : IAssemblyLocator
    {
        private readonly ICollection<string> _probingDirectories;

        public ICollection<string> ReferencePaths { get; }

        public ProbingAssemblyLocator(ICollection<string> probingDirectories)
        {
            this._probingDirectories = probingDirectories;
            this.ReferencePaths = new HashSet<string>();
        }

        public bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            path = this._probingDirectories
                       .Select(x => Path.Combine(x, $"{assemblyName}.dll"))
                       .FirstOrDefault(File.Exists);

            if (path != null)
            {
                this.ReferencePaths.Add(path);
                return true;
            }

            return false;
        }
    }
}