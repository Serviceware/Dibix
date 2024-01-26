using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    public sealed class HttpApiRegistry : IHttpApiRegistry
    {
        #region Fields
        private readonly ICollection<HttpApiDescriptor> _apis;
        private readonly IDictionary<Assembly, string> _areaNameCache;
        private readonly ICollection<HttpApiDescriptor> _declarativeApis;
        #endregion

        #region Constructor
        private HttpApiRegistry(ICollection<HttpApiDescriptor> apis)
        {
            this._apis = apis;
            this._areaNameCache = this._apis.GroupBy(x => new { Assembly = GetAssembly(x), x.AreaName }).ToDictionary(x => x.Key.Assembly, x => x.Key.AreaName);
            this._declarativeApis = this._apis.Where(IsDeclarative).ToArray();
        }
        #endregion

        #region Factory Methods
        public static IHttpApiRegistry Discover(IEnumerable<Assembly> additionalAssemblies) => Discover(CollectDiscoveryStrategies(additionalAssemblies));
        public static IHttpApiRegistry Discover(IHttpApiDiscoveryStrategy strategy) => Discover(EnumerableExtensions.Create(strategy));
        #endregion

        #region IHttpApiRegistry Members
        public string GetAreaName(Assembly assembly)
        {
            if (!this._areaNameCache.TryGetValue(assembly, out string areaName))
                throw new InvalidOperationException(String.Concat("Area not registered for assembly: ", assembly));

            return areaName;
        }

        public IEnumerable<HttpApiDescriptor> GetCustomApis() => this._declarativeApis;
        #endregion

        #region Private Methods
        private static IHttpApiRegistry Discover(IEnumerable<IHttpApiDiscoveryStrategy> strategies)
        {
            HttpApiDiscoveryContext context = new HttpApiDiscoveryContext();
            ICollection<HttpApiDescriptor> apis = strategies.SelectMany(x => x.Collect(context)).ToArray();
            context.FinishProxyAssembly();
            return new HttpApiRegistry(apis);
        }

        private static IEnumerable<IHttpApiDiscoveryStrategy> CollectDiscoveryStrategies(IEnumerable<Assembly> additionalAssemblies)
        {
            yield return new AssemblyHttpApiDiscoveryStrategy(additionalAssemblies);
        }

        private static Assembly GetAssembly(HttpApiDescriptor descriptor) => descriptor is HttpApiRegistration registration ? registration.Assembly : descriptor.GetType().Assembly;

        private static bool IsDeclarative(HttpApiDescriptor descriptor) => !(descriptor is HttpApiRegistration);
        #endregion
    }
}