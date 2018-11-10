using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IFileSystemProvider
    {
        IEnumerable<string> GetFilesInProject(string projectName, string virtualFolderPath, bool recursive, IEnumerable<string> excludedFolders);
        string GetPhysicalFilePath(string projectName, string virtualFilePath);
    }
}