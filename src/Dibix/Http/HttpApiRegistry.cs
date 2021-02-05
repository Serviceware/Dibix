using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dibix.Http
{
    public sealed class HttpApiRegistry : IHttpApiRegistry
    {
        #region Fields
        private readonly ICollection<HttpApiDescriptor> _apis;
        private readonly IDictionary<Assembly, string> _areaNameCache;
        private readonly ICollection<HttpApiDescriptor> _declarativeApis;
        #endregion

        #region Constructor
        public HttpApiRegistry(IEnumerable<Assembly> assemblies)
        {
            this._apis = CollectHttpApiDescriptors(assemblies);
            this._areaNameCache = this._apis.GroupBy(x => new { Assembly = GetAssembly(x), x.AreaName }).ToDictionary(x => x.Key.Assembly, x => x.Key.AreaName);
            this._declarativeApis = this._apis.Where(IsDeclarative).ToArray();
        }
        #endregion

        #region IHttpApiRegistry Members
        public string GetAreaName(Assembly assembly)
        {
            if (!this._areaNameCache.TryGetValue(assembly, out string areaName))
                throw new InvalidOperationException(String.Concat("Area not registered for assembly: ", assembly));

            return areaName;
        }

//#pragma warning disable CA1024 // Use properties where appropriate
        public IEnumerable<HttpApiDescriptor> GetCustomApis()
//#pragma warning restore CA1024 // Use properties where appropriate
        {
            return this._declarativeApis;
        }
        #endregion

        #region Private Methods
        private static ICollection<HttpApiDescriptor> CollectHttpApiDescriptors(IEnumerable<Assembly> assemblies)
        {
            HttpApiDiscoveryContext context = new HttpApiDiscoveryContext();
            ICollection<HttpApiDescriptor> descriptors = new Collection<HttpApiDescriptor>();
            foreach (Assembly assembly in assemblies)
            {
                ApiRegistrationAttribute attribute = assembly.GetCustomAttribute<ApiRegistrationAttribute>();
                if (attribute == null)
                    continue;

                if (String.IsNullOrEmpty(attribute.AreaName))
                    throw new InvalidOperationException($"Area name in api registration cannot be empty: {assembly.GetName().Name}");

                ICollection<Type> types = GetLoadableTypes(assembly).ToArray();

                Type apiDescriptorType = types.FirstOrDefault(typeof(HttpApiDescriptor).IsAssignableFrom);
                HttpApiDescriptor descriptor = apiDescriptorType != null ? (HttpApiDescriptor)Activator.CreateInstance(apiDescriptorType) : new HttpApiRegistration(assembly);
                descriptor.Configure(context);
                descriptors.Add(descriptor);
            }

            context.FinishProxyAssembly();

            return descriptors;
        }

        private static Assembly GetAssembly(HttpApiDescriptor descriptor) => descriptor is HttpApiRegistration registration ? registration.Assembly : descriptor.GetType().Assembly;
        private static bool IsDeclarative(HttpApiDescriptor descriptor) => !(descriptor is HttpApiRegistration);
        #endregion

        #region Private Methods
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        #endregion

        #region Nested Types
        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            public Assembly Assembly { get; }

            public HttpApiRegistration(Assembly assembly) => this.Assembly = assembly;

            public override void Configure(IHttpApiDiscoveryContext context) { }

            protected override string ResolveAreaName(Assembly assembly) => base.ResolveAreaName(this.Assembly);
        }

        private sealed class HttpApiDiscoveryContext : IHttpApiDiscoveryContext
        {
#if !NETSTANDARD
            private readonly Lazy<ReflectionHttpActionTargetProxyBuilder> _proxyBuilderAccessor;
            private readonly ICollection<ProxyMethodEntry> _proxyTargetHandlerMap;

            public HttpApiDiscoveryContext()
            {
                this._proxyBuilderAccessor = new Lazy<ReflectionHttpActionTargetProxyBuilder>(ReflectionHttpActionTargetProxyBuilder.Create);
                this._proxyTargetHandlerMap = new Collection<ProxyMethodEntry>();
            }
            
#endif
            public void RegisterProxyHandler(MethodInfo method, Action<MethodInfo> targetHandler)
            {
                if (!NeedsProxy(method))
                    return;

#if NETSTANDARD
                throw new PlatformNotSupportedException("Dynamic proxy method generation is not supported on .NET standard (yet)");
#else
                this._proxyBuilderAccessor.Value.AddMethod(method);
                this._proxyTargetHandlerMap.Add(new ProxyMethodEntry(method, targetHandler));
#endif
            }

            public void FinishProxyAssembly()
            {
#if !NETSTANDARD
                foreach (ProxyMethodEntry registration in this._proxyTargetHandlerMap)
                {
                    ProxyMethodEntry entry = registration;
                    MethodInfo proxyMethod = this._proxyBuilderAccessor.Value.GetProxyMethod(entry.Method);
                    entry.Method = proxyMethod;
                }
#endif
            }

            private static bool NeedsProxy(MethodBase method) => method.GetParameters().Any(x => x.ParameterType.IsByRef);
        }

        private struct ProxyMethodEntry
        {
            private MethodInfo _method;
            private readonly Action<MethodInfo> _methodUpdater;

            public MethodInfo Method
            {
                get => this._method;
                set
                {
                    this._method = value;
                    this._methodUpdater(value);
                }
            }

            public ProxyMethodEntry(MethodInfo method, Action<MethodInfo> methodUpdater)
            {
                this._method = method;
                this._methodUpdater = methodUpdater;
            }
        }
        #endregion
    }
}