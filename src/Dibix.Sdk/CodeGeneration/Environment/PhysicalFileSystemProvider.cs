using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PhysicalFileSystemProvider : IFileSystemProvider
    {
        public string CurrentDirectory { get; }

        public PhysicalFileSystemProvider(string currentDirectory)
        {
            this.CurrentDirectory = currentDirectory;
        }

        public string GetPhysicalFilePath(string root, VirtualPath virtualPath)
        {
            string path = Path.GetFullPath(Path.Combine(this.CurrentDirectory, root, virtualPath));
            return path;
        }

        public IEnumerable<string> GetFiles(string root, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude)
        {
            ICollection<string> normalizedExclude = exclude.Select(x => (string)x).ToArray();
            foreach (VirtualPath virtualPath in include)
            {
                string path = this.GetPhysicalFilePath(root, virtualPath);
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
    }
}