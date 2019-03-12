using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IFileSystemProvider
    {
        string CurrentDirectory { get; }

        string GetPhysicalFilePath(string root, VirtualPath virtualPath);
        IEnumerable<string> GetFiles(string root, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude);
    }
}