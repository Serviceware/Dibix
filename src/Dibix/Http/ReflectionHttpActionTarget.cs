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

        public static IHttpActionTarget Create(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException($"Could not find a public static method named '{methodName}' on {type}");

            return new ReflectionHttpActionTarget(method);
        }

        public static IHttpActionTarget Create(string assemblyAndTypeQualifiedMethodName)
        {
            // DataImport.Business.ExternalController#ExternalAction,DataImport.Business.Implementation
            string[] targetParts = assemblyAndTypeQualifiedMethodName.Split(',');
            if (targetParts.Length != 2)
                throw new InvalidOperationException($"Invalid action target format: {assemblyAndTypeQualifiedMethodName}");

            int typeNameIndex = targetParts[0].LastIndexOf('.');
            if (typeNameIndex < 0)
                throw new InvalidOperationException($"Invalid action target format: {assemblyAndTypeQualifiedMethodName}");

            string assemblyName = targetParts[1];
            string typeName = targetParts[0].Substring(0, typeNameIndex);
            string methodName = targetParts[0].Substring(typeNameIndex + 1);

            string assemblyQualifiedTypeName = $"{typeName},{assemblyName}";
            Type type = Type.GetType(assemblyQualifiedTypeName, true);
            return Create(type, methodName);
        }
    }
}