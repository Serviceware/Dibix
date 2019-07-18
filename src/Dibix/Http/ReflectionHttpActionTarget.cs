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
            // DataImport.Business.ExternalController#ExternalAction,DataImport.Business.Implementation
            string[] targetParts = externalTarget.Split(',');
            if (targetParts.Length != 2)
                throw new InvalidOperationException($"Invalid action target format: {externalTarget}");

            int typeNameIndex = targetParts[0].LastIndexOf('.');
            if (typeNameIndex < 0)
                throw new InvalidOperationException($"Invalid action target format: {externalTarget}");

            string assemblyName = targetParts[1];
            string typeName = targetParts[0].Substring(0, typeNameIndex);
            string methodName = targetParts[0].Substring(typeNameIndex + 1);

            string assemblyQualifiedTypeName = $"{typeName},{assemblyName}";
            Type type = Type.GetType(assemblyQualifiedTypeName, true);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException($"Could not find a public static method named '{methodName}' on {assemblyQualifiedTypeName}");

            return new ReflectionHttpActionTarget(method);
        }
    }
}