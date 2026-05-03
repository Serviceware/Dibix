using System;
using System.IO;

namespace Dibix.Sdk
{
    internal static class NuGetUtility
    {
        public static string PackageCacheDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    }
}