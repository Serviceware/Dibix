using System;
using System.Reflection;

namespace Dibix.Http
{
    public sealed class ReflectionHttpActionTarget : IHttpActionTarget
    {
        private readonly MethodInfo _methodInfo;

        private ReflectionHttpActionTarget(MethodInfo methodInfo)
        {
            this._methodInfo = methodInfo;
        }

        MethodInfo IHttpActionTarget.Build()
        {
            return this._methodInfo;
        }

        public static IHttpActionTarget Create(MethodInfo methodInfo)
        {
            return new ReflectionHttpActionTarget(methodInfo);
        }

        public static IHttpActionTarget Create(string externalTarget)
        {
            // TODO: DataImport.Business.ExternalController#ExternalAction,DataImport.Business.Implementation
            throw new NotSupportedException();
        }
    }
}