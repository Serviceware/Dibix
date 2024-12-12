using System;

namespace Dibix.Http.Server.AspNet
{
    public sealed class ExternalReflectionHttpActionTarget : ReflectionHttpActionTarget
    {
        public ExternalReflectionHttpActionTarget(IHttpApiDiscoveryContext context, Type type, string methodName) : base(context, type, methodName) { }

        public static IHttpActionTarget Create(string assemblyAndTypeQualifiedMethodName) => Create(null, assemblyAndTypeQualifiedMethodName);
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
            return new ExternalReflectionHttpActionTarget(context, type, methodName);
        }
    }
}