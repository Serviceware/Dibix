using System.IO;

namespace Dibix.Dac.Extensions
{
    internal static class DependentAssemblyLocator
    {
        private const string RulesAssemblyName = "Dibix.Sdk";
        private const string RulesAssemblyPlatform = "net451";

        public static string LocateRulesAssembly(string currentLocation) => LocatePackage(currentLocation, RulesAssemblyName, RulesAssemblyPlatform);

        private static string LocatePackage(string currentLocation, string packageName, string packagePlatform)
        {
            string root = RootDirectoryLocator.LocateRootDirectory(currentLocation);
            if (Directory.Exists(Path.Combine(root, PackageLocator.PackagesDirectory)))
                return new LocalNugetPackageLocator(root).LocatePackage(packageName, packagePlatform);

            return new GlobalNugetPackageLocator().LocatePackage(packageName, packagePlatform);
        }
    }
}