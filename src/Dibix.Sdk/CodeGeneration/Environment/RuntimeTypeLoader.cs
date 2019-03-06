using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class RuntimeTypeLoader : ITypeLoader
    {
        private readonly IAssemblyLocator _assemblyLocator;

        public RuntimeTypeLoader(IAssemblyLocator assemblyLocator)
        {
            this._assemblyLocator = assemblyLocator;
        }

        public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
        {
            try
            {
                TypeInfo info;
                if (!String.IsNullOrEmpty(typeName.AssemblyName))
                {
                    this._assemblyLocator.TryGetAssemblyLocation(typeName.AssemblyName, out string assemblyLocation);
                    info = ReflectionTypeLoader.GetTypeInfo(typeName, assemblyLocation);
                }
                else
                {
                    Type type = Type.GetType(typeName.NormalizedTypeName, true);
                    info = TypeInfo.FromClrType(type, typeName);
                }
                return info;
            }
            catch (Exception ex)
            {
                errorHandler(ex.Message);
                return null;
            }
        }
    }
}