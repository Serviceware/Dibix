using System;
using System.Collections.Concurrent;
using System.IO;

namespace Dibix.Sdk.Cli.Tools
{
    internal static class PackageUtility
    {
        private static readonly ConcurrentDictionary<string, ConsumerPackageManager> ConsumerPackageManagers = new ConcurrentDictionary<string, ConsumerPackageManager>();

        public static string PackageCacheDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        public static string[] NuGetPackageNames { get; } =
        [
            "Dibix",
            "Dibix.Dapper",
            "Dibix.Http.Client",
            "Dibix.Http.Server",
            "Dibix.Http.Server.AspNet",
            "Dibix.Http.Server.AspNetCore",
            "Dibix.Sdk",
            "Dibix.Testing",
            "Dibix.Worker.Abstractions"
        ];

        public static ConsumerPackageManager GetPackageManagerForConsumer(string consumerDirectory)
        {
            ConsumerPackageManager packageManager = ConsumerPackageManagers.GetOrAdd(consumerDirectory, ConsumerPackageManager.Load);
            return packageManager;
        }
    }
}