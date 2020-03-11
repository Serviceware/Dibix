using System;
using System.Reflection;

namespace Dibix
{
    internal static class TypeExtensions
    {
        public static bool IsPrimitive(this Type type)
        {
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                type = nullableType;

            TypeInfo typeInfo = type.GetTypeInfo();
            return typeInfo.IsPrimitive || typeInfo.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime);
        }
    }
}