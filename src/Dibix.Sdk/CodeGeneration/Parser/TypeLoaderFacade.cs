using System;
using System.Collections.Concurrent;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class TypeLoaderFacade
    {
        private static readonly ConcurrentDictionary<string, TypeInfo> TypeCache = new ConcurrentDictionary<string, TypeInfo>();
        private static readonly RuntimeTypeLoader RuntimeTypeLoader = new RuntimeTypeLoader();

        public static TypeInfo LoadType(string typeName, IExecutionEnvironment environment, Action<string> errorHandler)
        {
            TypeName parsedTypeName = typeName;

            // Normally we would cache the type information, however T4 templates are only "recompiled" if the actual T4 content changes.
            // This isn't the case, so any static cache will still be in proc. That means also new members to an entity won't take effect.
            /*return TypeCache.GetOrAdd(normalizedTypeName, x =>
            {*/
                if (!parsedTypeName.IsAssemblyQualified)
                {
                    TypeInfo clrType = TryClrType(parsedTypeName);
                    if (clrType != null)
                        return clrType;
                }

                ITypeLoader typeLoader = parsedTypeName.IsAssemblyQualified ? RuntimeTypeLoader : (ITypeLoader)environment;
                return typeLoader.LoadType(environment, parsedTypeName, errorHandler);
            //});
        }

        private static TypeInfo TryClrType(TypeName parsedTypeName)
        {
            // Try CSharp type name first (string => System.String)
            Type type = parsedTypeName.NormalizedTypeName.ToClrType();
            if (type != null)
                return new TypeInfo(parsedTypeName, true);

            type = Type.GetType(parsedTypeName.NormalizedTypeName);
            if (type != null && type.IsPrimitive())
            {
                parsedTypeName.CSharpTypeName = type.ToCSharpTypeName();
                return new TypeInfo(parsedTypeName, true);
            }

            return null;
        }
    }
}