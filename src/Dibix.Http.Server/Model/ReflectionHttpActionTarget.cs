using System;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class ReflectionHttpActionTarget : IHttpActionTarget
    {
        private MethodInfo _methodInfo;
        
        protected ReflectionHttpActionTarget(IHttpApiDiscoveryContext context, Type type, string methodName)
        {
            _methodInfo = GetMethod(type, methodName);
            context?.RegisterProxyHandler(_methodInfo, this.UpdateMethod);
        }

        MethodInfo IHttpActionTarget.Build() => _methodInfo;

        private void UpdateMethod(MethodInfo method) => _methodInfo = method;

        private static MethodInfo GetMethod(Type type, string methodName) => type.SafeGetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
    }
}