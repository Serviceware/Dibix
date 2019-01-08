using System;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class RuntimeTypeLoader : ITypeLoader
    {
        public TypeInfo LoadType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler)
        {
            Type type = GetType(environment, typeName, errorHandler);
            if (type == null)
                return null;

            typeName.ClrType = type;
            TypeInfo info = new TypeInfo(typeName, type.IsPrimitive());
            foreach (PropertyInfo property in type.GetProperties())
                info.Properties.Add(property.Name);

            return info;
        }

        private static Type GetType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler)
        {
            try
            {
                Type type;
                if (!String.IsNullOrEmpty(typeName.AssemblyName))
                {
                    Assembly assembly = environment.LoadAssembly(typeName.AssemblyName);
                    type = assembly.GetType(typeName.NormalizedTypeName, true);
                }
                else
                    type = Type.GetType(typeName.NormalizedTypeName, true);

                return type;
            }
            catch (Exception ex)
            {
                errorHandler(ex.Message);
                return null;
            }
        }
    }
}