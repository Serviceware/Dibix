using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class HttpApiDiscoveryContext : IHttpApiDiscoveryContext
    {
        private readonly Lazy<ReflectionHttpActionTargetProxyBuilder> _proxyBuilderAccessor;
        private readonly ICollection<ProxyMethodEntry> _proxyTargetHandlerMap;

        public HttpApiDiscoveryContext()
        {
            this._proxyBuilderAccessor = new Lazy<ReflectionHttpActionTargetProxyBuilder>(ReflectionHttpActionTargetProxyBuilder.Create);
            this._proxyTargetHandlerMap = new Collection<ProxyMethodEntry>();
        }
            
        public void RegisterProxyHandler(MethodInfo method, Action<MethodInfo> targetHandler)
        {
            if (!NeedsProxy(method))
                return;

            this._proxyBuilderAccessor.Value.AddMethod(method);
            this._proxyTargetHandlerMap.Add(new ProxyMethodEntry(method, targetHandler));
        }

        public void FinishProxyAssembly()
        {
            foreach (ProxyMethodEntry registration in this._proxyTargetHandlerMap)
            {
                ProxyMethodEntry entry = registration;
                MethodInfo proxyMethod = this._proxyBuilderAccessor.Value.GetProxyMethod(entry.Method);
                entry.Method = proxyMethod;
            }
        }

        private static bool NeedsProxy(MethodBase method) => method.GetParameters().Any(x => x.ParameterType.IsByRef);
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
    }
}