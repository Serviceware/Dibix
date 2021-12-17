using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk
{
    internal static class TypeExtensions
    {
        private static readonly IDictionary<string, Type> CSharpTypeNames = new Dictionary<string, Type>
        {
            ["object"]  = typeof(object)
          , ["string"]  = typeof(string)
          , ["bool"]    = typeof(bool)
          , ["byte"]    = typeof(byte)
          , ["char"]    = typeof(char)
          , ["decimal"] = typeof(decimal)
          , ["double"]  = typeof(double)
          , ["short"]   = typeof(short)
          , ["int"]     = typeof(int)
          , ["long"]    = typeof(long)
          , ["sbyte"]   = typeof(sbyte)
          , ["float"]   = typeof(float)
          , ["ushort"]  = typeof(ushort)
          , ["uint"]    = typeof(uint)
          , ["ulong"]   = typeof(ulong)
          , ["void"]    = typeof(void)
        };

        public static Type ToClrType(this string cSharpTypeName)
        {
            bool isArray = cSharpTypeName.EndsWith("[]", StringComparison.Ordinal);
            if (isArray)
                cSharpTypeName = cSharpTypeName.Substring(0, cSharpTypeName.Length - 2);

            if (CSharpTypeNames.TryGetValue(cSharpTypeName, out Type clrType) && isArray) 
                clrType = clrType.MakeArrayType();

            return clrType;
        }

        public static bool IsNullable(this PropertyInfo property) => IsNullable(property.PropertyType, property.DeclaringType, property.CustomAttributes);
        public static bool IsNullable(this FieldInfo field) => IsNullable(field.FieldType, field.DeclaringType, field.CustomAttributes);
        public static bool IsNullable(this ParameterInfo parameter) => IsNullable(parameter.ParameterType, parameter.Member, parameter.CustomAttributes);
        private static bool IsNullable(Type memberType, MemberInfo declaringType, IEnumerable<CustomAttributeData> customAttributes)
        {
            if (memberType.IsValueType)
                return Nullable.GetUnderlyingType(memberType) != null;

            CustomAttributeData nullable = customAttributes.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
            if (nullable != null && nullable.ConstructorArguments.Count == 1)
            {
                CustomAttributeTypedArgument attributeArgument = nullable.ConstructorArguments[0];
                if (attributeArgument.ArgumentType == typeof(byte[]))
                {
                    ReadOnlyCollection<CustomAttributeTypedArgument> args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value;
                    if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                    {
                        return (byte)args[0].Value == 2;
                    }
                }
                else if (attributeArgument.ArgumentType == typeof(byte))
                {
                    return (byte)attributeArgument.Value == 2;
                }
            }

            for (MemberInfo type = declaringType; type != null; type = type.DeclaringType)
            {
                CustomAttributeData context = type.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
                if (context != null
                 && context.ConstructorArguments.Count == 1
                 && context.ConstructorArguments[0].ArgumentType == typeof(byte))
                {
                    return (byte)context.ConstructorArguments[0].Value == 2;
                }
            }

            // Couldn't find a suitable attribute
            return false;
        }
    }
}