using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk
{
    internal sealed class PhysicalFileSystemProvider : IFileSystemProvider
    {
        private readonly string _currentDirectory;

        public PhysicalFileSystemProvider(string currentDirectory)
        {
            this._currentDirectory = currentDirectory;
        }

        public IEnumerable<string> GetFilesInProject(string projectName, string virtualFolderPath, bool recursive, IEnumerable<string> excludedFolders)
        {
            string relativePath = virtualFolderPath != null ? virtualFolderPath.Replace("/", "\\").TrimEnd('\\') : String.Empty;
            string directory = Path.GetFullPath(Path.Combine(this._currentDirectory, relativePath));
            string[] paths = Directory.EnumerateFiles(directory, "*.sql", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                      .Where(x => !excludedFolders.Any(y => x.Substring(directory.Length + 1).StartsWith(y)))
                                      .ToArray();

            return paths;
        }

        public string GetPhysicalFilePath(string projectName, string virtualFilePath)
        {
            string path = Path.GetFullPath(Path.Combine(this._currentDirectory, virtualFilePath));
            return path;
        }
    }
}