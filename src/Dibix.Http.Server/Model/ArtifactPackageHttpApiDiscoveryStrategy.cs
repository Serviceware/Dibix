using System.Collections.Generic;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class ArtifactPackageHttpApiDiscoveryStrategy : IHttpApiDiscoveryStrategy
    {
        #region IHttpApiDiscoveryStrategy Members
        public IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context)
        {
            // TODO
            // Move all the declarative information from the DLL to the package.
            // Then implement this strategy based on the package metadata.
            return new AssemblyHttpApiDiscoveryStrategy(CollectAssemblies()).Collect(context);
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<Assembly> CollectAssemblies();
        #endregion

        #region Protected Methods
        protected byte[] Unwrap(string packagePath)
        {
            byte[] content = ArtifactPackage.GetContent(packagePath);
            return content;
        }
        #endregion
    }
}