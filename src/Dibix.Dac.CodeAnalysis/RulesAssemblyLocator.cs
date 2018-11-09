using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Dac.CodeAnalysis
{
    internal static class RulesAssemblyLocator
    {
        private const string PackagesFolder = "packages";
        private const string PackagesSubFolder = @"lib\net451";
        private const string PackageName = "Dibix.Dac.CodeAnalysis.Rules";
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
            } while (current != null && !IsRootFolder(current));

            if (current == null)
                throw new InvalidOperationException($@"Could not determine root of source control.
We are currently looking for any of these files: 
{String.Join(", ", RootMarkers)}");

            return LocatePackagePath(current);
        }

        private static bool IsRootFolder(DirectoryInfo folder)
        {
            return folder.EnumerateFiles()
                         .Any(x => RootMarkerRegex.IsMatch(x.Name));
        }

        private static string LocatePackagePath(DirectoryInfo folder)
        {
            string packagesPath = Path.Combine(folder.FullName, PackagesFolder);
            string searchPattern = $"{PackageName}.?.?.?";
            string packagePath = Directory.EnumerateDirectories(packagesPath, searchPattern)
                                          .OrderByDescending(x => x)
                                          .Select(x => Path.Combine(x, PackagesSubFolder, String.Concat(PackageName, ".dll")))
                                          .FirstOrDefault();
            if (packagePath == null)
                throw new InvalidOperationException($@"Could not determine code analysis rules assembly in the following location:
{Path.Combine(packagesPath, searchPattern, PackagesSubFolder, String.Concat(PackageName, ".dll"))}");

            return packagePath;
        }
    }
}