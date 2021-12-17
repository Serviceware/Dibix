using System;
#if !NET5_0
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endif
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class HttpApiDiscoveryContext : IHttpApiDiscoveryContext
    {
#if !NET5_0
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

#if NET5_0
            throw new PlatformNotSupportedException("Dynamic proxy method generation is not supported on .NET standard (yet)");
#else
                this._proxyBuilderAccessor.Value.AddMethod(method);
                this._proxyTargetHandlerMap.Add(new ProxyMethodEntry(method, targetHandler));
#endif
        }

        public void FinishProxyAssembly()
        {
#if !NET5_0
                foreach (ProxyMethodEntry registration in this._proxyTargetHandlerMap)
                {
                    ProxyMethodEntry entry = registration;
                    MethodInfo proxyMethod = this._proxyBuilderAccessor.Value.GetProxyMethod(entry.Method);
                    entry.Method = proxyMethod;
                }
#endif
        }

        private static bool NeedsProxy(MethodBase method) => method.GetParameters().Any(x => x.ParameterType.IsByRef);
#if !NET5_0

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
#endif
    }
}