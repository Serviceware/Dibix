namespace Dibix.Dac.Extensions
{
    internal sealed class LocalNugetPackageLocator : PackageLocator
    {
        private readonly string _rootDirectory;

        public LocalNugetPackageLocator(string rootDirectory)
        {
            this._rootDirectory = rootDirectory;
        }

        protected override string GetPackagesRoot() => this._rootDirectory;
        protected override string GetSearchPattern(string packageName) => $"{packageName}.*";
    }
}