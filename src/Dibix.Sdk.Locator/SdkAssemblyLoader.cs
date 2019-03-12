using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk
{
    internal static class SdkAssemblyLoader
    {
        private const string RulesAssemblyName = "Dibix.Sdk";
        private const string TempFolderPrefix = "dibix-sdk-";

        public static Assembly Load(string startDirectory)
        {
            string sourcePath = LocatePackage(startDirectory, RulesAssemblyName);
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

        private static string LocatePackage(string startDirectory, string packageName)
        {
            string root = RootDirectoryLocator.LocateRootDirectory(startDirectory);

            const string folderName = "tools";

            if (Directory.Exists(Path.Combine(root, PackageLocator.PackagesDirectoryName)))
                return new LocalNugetPackageLocator(root).LocatePackage(packageName, folderName);

            return new GlobalNugetPackageLocator().LocatePackage(packageName, folderName);
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