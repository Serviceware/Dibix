using System;
using System.Reflection;

namespace Dibix.Http
{
    public interface IHttpApiDiscoveryContext
    {
        void RegisterProxyHandler(MethodInfo method, Action<MethodInfo> targetHandler);
    }
}