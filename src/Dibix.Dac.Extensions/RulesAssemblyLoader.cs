using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Dac.Extensions
{
    internal static class RulesAssemblyLoader
    {
        private const string RulesAssemblyName = "Dibix.Sdk";
        private const string RulesAssemblyPlatform = "net451";
        private const string TempFolderPrefix = "dibix-sdk-";

        public static Assembly Load(string currentLocation)
        {
            string sourcePath = LocatePackage(currentLocation, RulesAssemblyName, RulesAssemblyPlatform);
            string fileName = Path.GetFileName(sourcePath);
            string tempDirectory = Path.GetTempPath();
            if (!TryGetExistingTempFile(tempDirectory, sourcePath, fileName, out string targetPath))
            {
                string targetDirectory = Path.Combine(tempDirectory, $"{TempFolderPrefix}{Guid.NewGuid()}");
                Directory.CreateDirectory(targetDirectory);
                targetPath = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, targetPath);
            }
            return Assembly.LoadFrom(targetPath);
        }

        private static string LocatePackage(string currentLocation, string packageName, string packagePlatform)
        {
            string root = RootDirectoryLocator.LocateRootDirectory(currentLocation);
            if (Directory.Exists(Path.Combine(root, PackageLocator.PackagesDirectory)))
                return new LocalNugetPackageLocator(root).LocatePackage(packageName, packagePlatform);

            return new GlobalNugetPackageLocator().LocatePackage(packageName, packagePlatform);
        }

        private static bool TryGetExistingTempFile(string tempDirectory, string sourcePath, string fileName, out string targetPath)
        {
            foreach (string targetDirectory in Directory.EnumerateDirectories(tempDirectory, $"{TempFolderPrefix}*"))
            {
                targetPath = Path.Combine(targetDirectory, fileName);
                if (File.Exists(targetPath) && FileEquals(sourcePath, targetPath))
                    return true;
            }
            targetPath = null;
            return false;
        }
        
        private static bool FileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            return file1.Length == file2.Length && !file1.Where((t, i) => t != file2[i]).Any();
        }
    }
}