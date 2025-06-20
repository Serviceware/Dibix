using System;
using System.IO;

namespace Dibix.Http.Host
{
    internal static class ApplicationEnvironment
    {
        public static string PackagesDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Packages");
        public const string PackageExtension = "dbx";
    }
}