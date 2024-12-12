using System.Collections.Generic;
using System.Linq;

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
        public static IHttpApiRegistry Discover(IHttpApiDiscoveryStrategy strategy)
        {
            HttpApiDiscoveryContext context = new HttpApiDiscoveryContext();
            ICollection<HttpApiDescriptor> apis = strategy.Collect(context).ToArray();
            context.FinishProxyAssembly();
            return new HttpApiRegistry(apis);
        }
        #endregion

        #region IHttpApiRegistry Members
        public IEnumerable<HttpApiDescriptor> GetApis() => _apis;
        #endregion
    }
}