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
        public static IHttpApiRegistry Discover(string directory) => Discover(directory, Enumerable.Empty<Assembly>());
        public static IHttpApiRegistry Discover(string directory, IEnumerable<Assembly> additionalAssemblies)
        {
            HttpApiDiscoveryContext context = new HttpApiDiscoveryContext();
            ICollection<HttpApiDescriptor> apis = CollectHttpApiDescriptors(context, directory, additionalAssemblies).ToArray();
            context.FinishProxyAssembly();
            return new HttpApiRegistry(apis);
        }
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
        private static IEnumerable<HttpApiDescriptor> CollectHttpApiDescriptors(IHttpApiDiscoveryContext context, string directory, IEnumerable<Assembly> additionalAssemblies)
        {
            IEnumerable<IHttpApiDiscoveryStrategy> strategies = CollectDiscoveryStrategies(directory, additionalAssemblies);
            IEnumerable<HttpApiDescriptor> descriptors = strategies.SelectMany(x => x.Collect(context));
            return descriptors;
        }

        private static IEnumerable<IHttpApiDiscoveryStrategy> CollectDiscoveryStrategies(string directory, IEnumerable<Assembly> additionalAssemblies)
        {
            yield return new ArtifactPackageHttpApiDiscoveryStrategy(directory);
            yield return new AssemblyHttpApiDiscoveryStrategy(additionalAssemblies);
        }

        private static Assembly GetAssembly(HttpApiDescriptor descriptor) => descriptor is HttpApiRegistration registration ? registration.Assembly : descriptor.GetType().Assembly;

        private static bool IsDeclarative(HttpApiDescriptor descriptor) => !(descriptor is HttpApiRegistration);
        #endregion
    }
}