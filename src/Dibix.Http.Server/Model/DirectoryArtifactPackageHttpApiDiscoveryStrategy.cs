using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class DirectoryArtifactPackageHttpApiDiscoveryStrategy : ArtifactPackageHttpApiDiscoveryStrategy, IHttpApiDiscoveryStrategy
    {
        #region Fields
        private readonly string _directory;
        #endregion

        #region Constructor
        public DirectoryArtifactPackageHttpApiDiscoveryStrategy(string directory)
        {
            this._directory = directory;
        }
        #endregion

        #region Overrides
        protected override IEnumerable<Assembly> CollectAssemblies()
        {
            return Directory.EnumerateFiles(this._directory, "*.dbx")
                            .Where(ShouldProcessPackagePath)
                            .Select(CollectAssembly);
        }
        #endregion

        #region Private Methods
        private static bool ShouldProcessPackagePath(string packagePath)
        {
            return true;
            //string artifactName = Path.GetFileNameWithoutExtension(packagePath);
            //bool assemblyLoaded = AppDomain.CurrentDomain
            //                               .GetAssemblies()
            //                               .Any(x => x.GetName().Name == artifactName);
            //return !assemblyLoaded;
        }

        private static Assembly CollectAssembly(string packagePath)
        {
            byte[] content = ArtifactPackage.GetContent(packagePath);
            Assembly assembly = Assembly.Load(content);
            return assembly;
        }
        #endregion
    }
}