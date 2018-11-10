using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Dac.Extensions
{
    internal static class RulesAssemblyLocator
    {
        private const string PackagesDirectory = "packages";
        private const string PackagesSubDirectory = @"lib\net451";
        private const string PackageName = "Dibix.Sdk";
        private static readonly string[] RootMarkers =
        {
            ".tfignore",
            ".gitignore"
        };
        private static readonly Regex RootMarkerRegex = new Regex(String.Join("|", RootMarkers.Select(Regex.Escape)), RegexOptions.Compiled);

        public static string Locate(string currentLocation)
        {
            DirectoryInfo current = new DirectoryInfo(currentLocation);
            do
            {
                current = current.Parent;
            } while (current != null && !IsRootDirectory(current));

            if (current == null)
                throw new InvalidOperationException($@"Could not determine root of source control.
We are currently looking for any of these files: 
{String.Join(", ", RootMarkers)}");

            return LocatePackagePath(current);
        }

        private static bool IsRootDirectory(DirectoryInfo directory)
        {
            return directory.EnumerateFiles()
                            .Any(x => RootMarkerRegex.IsMatch(x.Name));
        }

        private static string LocatePackagePath(DirectoryInfo rootDirectory)
        {
            string packagesDirectory = Path.Combine(rootDirectory.FullName, PackagesDirectory);
            if (Directory.Exists(packagesDirectory))
                return LocateLocalPackagePath(packagesDirectory);

            return LocateGlobalPackagePath(packagesDirectory);
        }

        private static string LocateLocalPackagePath(string packagesDirectory)
        {
            string searchPattern = $"{PackageName}.?.?.?";
            string packageFileName = String.Concat(PackageName, ".dll");
            string packagePath = Directory.EnumerateDirectories(packagesDirectory, searchPattern)
                                          .OrderByDescending(x => x)
                                          .Select(x => Path.Combine(x, PackagesSubDirectory, packageFileName))
                                          .FirstOrDefault();
            if (packagePath == null)
                throw new InvalidOperationException($@"Could not determine code analysis rules assembly in the following location:
{Path.Combine(packagesDirectory, searchPattern, PackagesSubDirectory, packageFileName)}");

            return packagePath;
        }

        private static string LocateGlobalPackagePath(string localPackagesDirectory)
        {
            string packageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", PackageName);
            if (!Directory.Exists(packageDirectory))
                throw new InvalidOperationException($@"Could not determine package directory in the following locations:
{Path.Combine(localPackagesDirectory, $"{PackageName}.?.?.?")}
{packageDirectory}");

            const string searchPattern = "?.?.?";
            string packageFileName = String.Concat(PackageName, ".dll");
            string packagePath = Directory.EnumerateDirectories(packageDirectory, searchPattern)
                                          .OrderByDescending(x => x)
                                          .Select(x => Path.Combine(x, PackagesSubDirectory, packageFileName))
                                          .FirstOrDefault();
            if (packagePath == null)
                throw new InvalidOperationException($@"Could not determine code analysis rules assembly in the following location:
{Path.Combine(packageDirectory, searchPattern, PackagesSubDirectory, packageFileName)}");

            return packagePath;
        }
    }
}