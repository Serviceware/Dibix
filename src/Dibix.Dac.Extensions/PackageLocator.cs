using System;
using System.IO;
using System.Linq;

namespace Dibix.Dac.Extensions
{
    internal abstract class PackageLocator
    {
        private const string LibDirectory = "lib";
        public const string PackagesDirectory = "packages";

        protected abstract string GetPackagesRoot();
        protected virtual string GetPackageSearchDirectory(string packageName) => null;
        protected abstract string GetSearchPattern(string packageName);

        public string LocatePackage(string packageName, string packagePlatform)
        {
            string packagesDirectory = Path.Combine(this.GetPackagesRoot(), PackagesDirectory);
            string relativePackageSearchDirectory = this.GetPackageSearchDirectory(packageName);
            string absolutePackageSearchDirectory = Path.Combine(packagesDirectory, relativePackageSearchDirectory ?? String.Empty);
            string subDirectory = Path.Combine(LibDirectory, packagePlatform);
            string searchPattern = this.GetSearchPattern(packageName);
            string packageFileName = String.Concat(packageName, ".dll");
            string packagePath = Directory.EnumerateDirectories(absolutePackageSearchDirectory, searchPattern)
                                          .OrderByDescending(x => x)
                                          .Select(x => Path.Combine(x, subDirectory, packageFileName))
                                          .FirstOrDefault();

            if (packagePath == null || !File.Exists(packagePath))
                throw new InvalidOperationException($@"Could not find package in the following location:
{Path.Combine(absolutePackageSearchDirectory, searchPattern, subDirectory, packageFileName)}");

            return packagePath;
        }
    }
}