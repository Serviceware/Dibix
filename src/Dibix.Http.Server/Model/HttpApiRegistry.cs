using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    public sealed class HttpApiRegistry : IHttpApiRegistry
    {
        #region Fields
        private readonly ICollection<HttpApiDescriptor> _apis;
        #endregion

        #region Constructor
        private HttpApiRegistry(ICollection<HttpApiDescriptor> apis)
        {
            _apis = apis;
        }
        #endregion

        #region Factory Methods
        public static IHttpApiRegistry Discover(IEnumerable<Assembly> additionalAssemblies) => DiscoverCore(new StaticAssemblyHttpApiDiscoveryStrategy(additionalAssemblies));
        public static IHttpApiRegistry Discover(IHttpApiDiscoveryStrategy strategy) => DiscoverCore(strategy);
        #endregion

        #region IHttpApiRegistry Members
        public IEnumerable<HttpApiDescriptor> GetApis() => _apis;
        #endregion

        #region Private Methods
        private static IHttpApiRegistry DiscoverCore(IHttpApiDiscoveryStrategy strategy)
        {
            HttpApiDiscoveryContext context = new HttpApiDiscoveryContext();
            ICollection<HttpApiDescriptor> apis = strategy.Collect(context).ToArray();
            context.FinishProxyAssembly();
            return new HttpApiRegistry(apis);
        }
        #endregion
    }
}