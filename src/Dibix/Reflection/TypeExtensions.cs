﻿using System;

namespace Dibix
{
    internal static class TypeExtensions
    {
        public static bool IsPrimitive(this Type type)
        {
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                type = nullableType;

            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(decimal)
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(DateTime);
        }
    }
}