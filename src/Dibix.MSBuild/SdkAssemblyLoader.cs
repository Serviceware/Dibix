using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.MSBuild
{
    internal static class SdkAssemblyLoader
    {
        private const string TempFolderPrefix = "dibix-sdk-";

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

            // Actually we would use Assembly.Load(AssemblyName.GetAssemblyName(filePath)) 
            // to make sure dependent assemblies are loaded from the current folder.
            // But we are trying to create a shadow copy to allow updates during running VS instances.
            // Since the assembly lies in the current folder, it would be loaded from here and lock the assembly.
            // Therefore we specifically override the logic and have to load our dependent assemblies ourselves.
            Assembly assembly = Assembly.LoadFrom(targetPath);

            if (!assembly.Location.StartsWith(tempDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unexpected location of Dibix.Sdk: {assembly.Location}");

            return assembly;
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