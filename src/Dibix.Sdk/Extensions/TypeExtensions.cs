using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk
{
    public static class TypeExtensions
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

        internal static Type ToClrType(this string cSharpTypeName)
        {
            bool isArray = cSharpTypeName.EndsWith("[]", StringComparison.Ordinal);
            if (isArray)
                cSharpTypeName = cSharpTypeName.Substring(0, cSharpTypeName.Length - 2);

            if (CSharpTypeNames.TryGetValue(cSharpTypeName, out Type clrType) && isArray) 
                clrType = clrType.MakeArrayType();

            return clrType;
        }

        internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            Guard.IsNotNull(assembly, nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        internal static bool IsDefined(this Assembly assembly, string fullAttributeTypeName)
        {
            Guard.IsNotNull(assembly, nameof(assembly));
            try
            {
                return assembly.CustomAttributes.Any(x => x.AttributeType.FullName == fullAttributeTypeName);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (TypeLoadException)
            {
                return false;
            }
        }
    }
}