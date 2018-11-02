using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Dibix.Sdk
{
    internal static class TypeLoaderFacade
    {
        private static readonly ConcurrentDictionary<string, TypeInfo> TypeCache = new ConcurrentDictionary<string, TypeInfo>();
        private static readonly RuntimeTypeLoader RuntimeTypeLoader = new RuntimeTypeLoader();

        public static TypeInfo LoadType(string typeName, IExecutionEnvironment environment, Action<string> errorHandler)
        {
            // Ignore nullable specifier to load type info
            // It will be used later in the generator
            string normalizedTypeName = typeName.TrimEnd('?');

            // Normally we would cache the type information, however T4 templates are only "recompiled" if the actual T4 content changes.
            // This isn't the case, so any static cache will still be in proc. That means also new members to an entity won't take effect.
            /*return TypeCache.GetOrAdd(normalizedTypeName, x =>
            {*/
                bool isMarkedExternal = normalizedTypeName.Contains(',');
                if (!isMarkedExternal)
                {
                    TypeInfo clrType = TryClrType(normalizedTypeName);
                    if (clrType != null)
                        return clrType;
                }
                return (isMarkedExternal ? RuntimeTypeLoader : (ITypeLoader)environment).LoadType(environment, typeName, normalizedTypeName, errorHandler);
            //});
        }

        private static TypeInfo TryClrType(string typeName)
        {
            // Try CSharp type name first (string => System.String)
            Type type = typeName.ToClrType();
            if (type != null)
                return new TypeInfo(typeName, true);

            type = Type.GetType(typeName);
            if (type != null && type.IsPrimitive())
                return new TypeInfo(type.ToCSharpTypeName(), true);

            return null;
        }
    }
}