using System.Collections.Generic;

namespace Dibix.Sdk
{
    public interface IFileSystemProvider
    {
        IEnumerable<string> GetFilesInProject(string projectName, string virtualFolderPath, bool recursive, IEnumerable<string> excludedFolders);
        string GetPhysicalFilePath(string projectName, string virtualFilePath);
    }
}