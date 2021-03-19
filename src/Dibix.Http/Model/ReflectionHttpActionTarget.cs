using System;
using System.Reflection;

namespace Dibix.Http
{
    public sealed class ReflectionHttpActionTarget : IHttpActionTarget
    {
        private MethodInfo _methodInfo;
        
        internal bool IsExternal { get; }

        private ReflectionHttpActionTarget(MethodInfo methodInfo, bool isExternal)
        {
            this.IsExternal = isExternal;
            this._methodInfo = methodInfo;
        }

        MethodInfo IHttpActionTarget.Build() => this._methodInfo;

        private void UpdateMethod(MethodInfo method) => this._methodInfo = method;

        public static IHttpActionTarget Create(Type type, string methodName) => Create(null, type, methodName);
        public static IHttpActionTarget Create(IHttpApiDiscoveryContext context, Type type, string methodName) => Create(context, type, methodName, isExternal: false);
        public static IHttpActionTarget Create(string assemblyAndTypeQualifiedMethodName) => Create((IHttpApiDiscoveryContext)null, assemblyAndTypeQualifiedMethodName);
        public static IHttpActionTarget Create(IHttpApiDiscoveryContext context, string assemblyAndTypeQualifiedMethodName)
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
            return Create(context, type, methodName, isExternal: true);
        }

        private static IHttpActionTarget Create(IHttpApiDiscoveryContext context, IReflect type, string methodName, bool isExternal)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException($"Could not find a public static method named '{methodName}' on {type}");

            ReflectionHttpActionTarget target = new ReflectionHttpActionTarget(method, isExternal);
            context?.RegisterProxyHandler(method, target.UpdateMethod);
            return target;
        }
    }
}