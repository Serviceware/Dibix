using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Dac.Extensions
{
    internal static class RootDirectoryLocator
    {
        private static string _rootDirectory;
        private static readonly string[] RootMarkers =
        {
            ".tfignore",
            ".gitignore"
        };
        private static readonly Regex RootMarkerRegex = new Regex(String.Join("|", RootMarkers.Select(Regex.Escape)), RegexOptions.Compiled);

        public static string LocateRootDirectory(string currentLocation)
        {
            if (_rootDirectory != null)
                return _rootDirectory;

            DirectoryInfo current = new DirectoryInfo(currentLocation);
            do
            {
                current = current.Parent;
            } while (current != null && !IsRootDirectory(current));

            if (current == null)
                throw new InvalidOperationException($@"Could not determine root of source control.
We are currently looking for any of these files: 
{String.Join(", ", RootMarkers)}");

            _rootDirectory = current.FullName;
            return _rootDirectory;
        }

        private static bool IsRootDirectory(DirectoryInfo directory)
        {
            return directory.EnumerateFiles()
                            .Any(x => RootMarkerRegex.IsMatch(x.Name));
        }
    }
}