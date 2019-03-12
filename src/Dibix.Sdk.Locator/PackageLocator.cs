using System;
using System.IO;
using System.Linq;

namespace Dibix.Sdk
{
    internal abstract class PackageLocator
    {
        public const string PackagesDirectoryName = "packages";

        protected abstract string GetPackagesRoot();
        protected virtual string GetPackageSearchDirectory(string packageName) => null;
        protected abstract string GetSearchPattern(string packageName);

        public string LocatePackage(string packageName, string folderName)
        {
            string packagesDirectory = Path.Combine(this.GetPackagesRoot(), PackagesDirectoryName);
            string relativePackageSearchDirectory = this.GetPackageSearchDirectory(packageName);
            string absolutePackageSearchDirectory = Path.Combine(packagesDirectory, relativePackageSearchDirectory ?? String.Empty);
            string searchPattern = this.GetSearchPattern(packageName);
            string packageFileName = String.Concat(packageName, ".dll");
            string packageDirectory = Directory.EnumerateDirectories(absolutePackageSearchDirectory, searchPattern)
                                               .OrderByDescending(x => x)
                                               .FirstOrDefault();

            string packagePath = null;
            if (!String.IsNullOrEmpty(packageDirectory))
            {
                string subDirectory = Path.Combine(packageDirectory, folderName);
                packagePath = Directory.EnumerateFiles(subDirectory, packageFileName, SearchOption.AllDirectories)
                                       .FirstOrDefault();
            }

            if (packagePath == null || !File.Exists(packagePath))
                throw new InvalidOperationException($@"Could not find package in the following location:
{Path.Combine(absolutePackageSearchDirectory, searchPattern, folderName, packageFileName)}");

            return packagePath;
        }
    }
}