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

        public static Assembly LocatePackageRootAndLoad(string startDirectory)
        {
            string rootDirectory = RootDirectoryLocator.LocateRootDirectory(startDirectory);
            string packagePath = LocatePackage(rootDirectory, RulesAssemblyName);
            return Load(packagePath);
        }

        public static Assembly Load(string packagePath)
        {
            string fileName = Path.GetFileName(packagePath);
            string tempDirectory = Path.GetTempPath();
            if (!TryGetExistingTempFile(tempDirectory, packagePath, fileName, out string targetPath))
            {
                string targetDirectory = Path.Combine(tempDirectory, $"{TempFolderPrefix}{Guid.NewGuid()}");
                Directory.CreateDirectory(targetDirectory);
                targetPath = Path.Combine(targetDirectory, Path.GetFileName(packagePath));
                File.Copy(packagePath, targetPath);
            }

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(targetPath);
            Assembly assembly = Assembly.Load(assemblyName);
            if (!assembly.Location.StartsWith(tempDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($@"Unexpected location of Dibix.Sdk: {assembly.Location}
This might happen, if the assembly was placed here manually, if so, please move it out of this location");
            }

            return assembly;
        }

        private static string LocatePackage(string rootDirectory, string packageName)
        {
            const string folderName = "lib";

            if (Directory.Exists(Path.Combine(rootDirectory, PackageLocator.PackagesDirectoryName)))
                return new LocalNugetPackageLocator(rootDirectory).LocatePackage(packageName, folderName);

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