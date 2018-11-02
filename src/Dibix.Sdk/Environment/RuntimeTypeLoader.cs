using System;
using System.Reflection;

namespace Dibix.Sdk
{
    internal sealed class RuntimeTypeLoader : ITypeLoader
    {
        public TypeInfo LoadType(IExecutionEnvironment environment, string typeName, string normalizedTypeName, Action<string> errorHandler)
        {
            Type type = GetType(environment, normalizedTypeName, errorHandler);
            if (type == null)
                return null;

            TypeInfo info = new TypeInfo(type.FullName, false);
            foreach (PropertyInfo property in type.GetProperties())
                info.Properties.Add(property.Name);

            return info;
        }

        private static Type GetType(IExecutionEnvironment environment, string typeName, Action<string> errorHandler)
        {
            // Ignore nullable specifier to load runtime type
            typeName = typeName.TrimEnd('?');

            // Try CSharp type name first (string => System.String)
            Type type = typeName.ToClrType();
            if (type != null)
                return type;

            try
            {
                string[] parts = typeName.Split(',');
                if (parts.Length > 1)
                {
                    Assembly assembly = environment.LoadAssembly(parts[1]);
                    type = assembly.GetType(parts[0], true);
                }
                else
                    type = Type.GetType(parts[0], true);

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