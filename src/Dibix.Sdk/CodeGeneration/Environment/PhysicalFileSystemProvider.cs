using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PhysicalFileSystemProvider : IFileSystemProvider
    {
        private readonly string _currentDirectory;

        public PhysicalFileSystemProvider(string currentDirectory)
        {
            this._currentDirectory = currentDirectory;
        }

        public string GetPhysicalFilePath(string projectName, VirtualPath virtualPath)
        {
            if (!String.IsNullOrEmpty(projectName))
                throw new ArgumentException($"The {nameof(PhysicalFileSystemProvider)} does not support project names", nameof(projectName));

            string path = this.GetPhysicalFilePath(virtualPath);
            return path;
        }

        public IEnumerable<string> GetFiles(string projectName, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude)
        {
            ICollection<string> normalizedExclude = exclude.Select(x => (string)x).ToArray();
            foreach (VirtualPath virtualPath in include)
            {
                string path = this.GetPhysicalFilePath(virtualPath);
                if (File.Exists(path))
                {
                    yield return path;
                }
                else
                {
                    if (!Directory.Exists(path))
                        throw new InvalidOperationException($"Invalid source path: {path}");

                    int excludePathStart = path.Length + 1;
                    foreach (string filePath in Directory.EnumerateFiles(path, "*.sql", virtualPath.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        if (normalizedExclude.Any(x => filePath.Substring(excludePathStart).StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        yield return filePath;
                    }
                }
            }
        }

        private string GetPhysicalFilePath(VirtualPath virtualPath)
        {
            string path = Path.GetFullPath(Path.Combine(this._currentDirectory, virtualPath));
            return path;
        }
    }
}