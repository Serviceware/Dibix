using System;

namespace Dibix.Http.Server
{
    public sealed class LocalReflectionHttpActionTarget : ReflectionHttpActionTarget
    {
        public LocalReflectionHttpActionTarget(IHttpApiDiscoveryContext context, Type type, string methodName) : base(context, type, methodName) { }

        public static IHttpActionTarget Create(Type type, string methodName) => Create(null, type, methodName);
        public static IHttpActionTarget Create(IHttpApiDiscoveryContext context, Type type, string methodName) => new LocalReflectionHttpActionTarget(context, type, methodName);
    }
}