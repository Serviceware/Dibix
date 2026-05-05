using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal static class PackageUtility
    {
        private static readonly ConcurrentDictionary<string, ConsumerPackageManager> ConsumerPackageManagers = new ConcurrentDictionary<string, ConsumerPackageManager>();


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

        public static async Task<string> GetLocalDibixVersion()
        {
            string dibixVersion = await ProcessUtility.Capture("nbgv", $"get-version --project \"{KnownDirectory.DibixRootDirectory}\" --variable NuGetPackageVersion").ConfigureAwait(false);
            return dibixVersion;
        }

        public static async Task CreateNuGetPackage(string packageName, string packageVersion, string configuration)
        {
            string projectName = packageName == "Dibix.Sdk" ? "Dibix.Sdk.Cli" : packageName;
            string sourcePath = Path.Combine(KnownDirectory.DibixRootDirectory, "src", projectName, $"{projectName}.csproj");
            await ProcessUtility.Execute("dotnet", $"pack \"{sourcePath}\" --verbosity quiet --nologo --no-restore -p:PackageVersionOverride={packageVersion} -p:Configuration={configuration}");
        }

        public static void RemovePackageFromNuGetPackageCache(string packageName, string packageVersion)
        {
            DirectoryInfo cacheDirectory = new DirectoryInfo(Path.Combine(KnownDirectory.PackageCacheDirectory, packageName, packageVersion));
            if (cacheDirectory.Exists)
                cacheDirectory.Delete(recursive: true);
        }

        public static void DeployPackageToNuGetPackageCache(string packageName, string packageVersion, string configuration)
        {
            string projectName = packageName == "Dibix.Sdk" ? "Dibix.Sdk.Cli" : packageName;
            string nupkgPath = Path.Combine(KnownDirectory.DibixRootDirectory, "src", projectName, "bin", configuration, $"{packageName}.{packageVersion}.nupkg");
            NuGetPackageExpander.Expand(packageName, packageVersion, nupkgPath, KnownDirectory.PackageCacheDirectory);
        }

        public static bool IsSdk(string packageName) => packageName == "Dibix.Sdk";
    }
}