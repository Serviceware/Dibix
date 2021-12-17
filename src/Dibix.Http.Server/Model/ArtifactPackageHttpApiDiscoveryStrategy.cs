using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class ArtifactPackageHttpApiDiscoveryStrategy : IHttpApiDiscoveryStrategy
    {
        #region Fields
        private readonly string _directory;
        #endregion

        #region Constructor
        public ArtifactPackageHttpApiDiscoveryStrategy(string directory)
        {
            this._directory = directory;
        }
        #endregion

        #region IHttpApiDiscoveryStrategy Members
        public IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context)
        {
            // TODO
            // Move all the declarative information from the DLL to the package.
            // Then implement this strategy based on the package metadata.
            return new AssemblyHttpApiDiscoveryStrategy(CollectAssemblies()).Collect(context);
        }
        #endregion

        #region Private Methods
        private IEnumerable<Assembly> CollectAssemblies()
        {
            return Directory.EnumerateFiles(this._directory, "*.dbx")
                            .Where(ShouldProcessPackagePath)
                            .Select(CollectAssembly);
        }

        private static bool ShouldProcessPackagePath(string packagePath)
        {
            string artifactName = Path.GetFileNameWithoutExtension(packagePath);
            bool assemblyLoaded = AppDomain.CurrentDomain
                                           .GetAssemblies()
                                           .Any(x => x.GetName().Name == artifactName);
            return !assemblyLoaded;
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