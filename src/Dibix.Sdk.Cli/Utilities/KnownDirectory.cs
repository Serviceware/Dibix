using System;
using System.IO;

namespace Dibix.Sdk.Cli
{
    internal static class KnownDirectory
    {
        public static string PackageCacheDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        public static string DibixRootDirectory { get; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}