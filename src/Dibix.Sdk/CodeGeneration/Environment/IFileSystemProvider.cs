﻿using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IFileSystemProvider
    {
        string CurrentDirectory { get; }

        string GetPhysicalFilePath(string projectName, VirtualPath virtualPath);
        IEnumerable<string> GetFiles(string projectName, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude);
    }
}