using System;
using System.Reflection;

namespace Dibix.Http.Server
{
    public interface IHttpApiDiscoveryContext
    {
        void RegisterProxyHandler(MethodInfo method, Action<MethodInfo> targetHandler);
    }
}