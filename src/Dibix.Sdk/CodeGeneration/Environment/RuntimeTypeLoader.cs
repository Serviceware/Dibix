using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class RuntimeTypeLoader : ITypeLoader
    {
        public TypeInfo LoadType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler)
        {
            try
            {
                TypeInfo info;
                if (!String.IsNullOrEmpty(typeName.AssemblyName))
                {
                    environment.TryGetAssemblyLocation(typeName.AssemblyName, out string assemblyLocation);
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