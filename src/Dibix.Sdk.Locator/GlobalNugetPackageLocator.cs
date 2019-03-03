using System;
using System.IO;

namespace Dibix.Sdk
{
    internal sealed class GlobalNugetPackageLocator : PackageLocator
    {
        protected override string GetPackagesRoot() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget");
        protected override string GetPackageSearchDirectory(string packageName) => packageName;
        protected override string GetSearchPattern(string packageName) => "*";
    }
}