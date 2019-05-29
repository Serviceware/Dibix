using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace Dibix.Sdk
{
    public static class TypeExtensions
    {
        private static readonly IDictionary<string, Type> CSharpTypeNames = LoadCSharpTypeNames().ToDictionary(x => x.Key, x => x.Value);

        internal static bool IsNullable(this Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        internal static Type MakeNullable(this Type type)
        {
            Type nullableType = typeof(Nullable<>).MakeGenericType(type);
            return nullableType;
        }

        public static string ToCSharpTypeName(this Type clrType)
        {
            Type nullableType = Nullable.GetUnderlyingType(clrType);
            if (nullableType != null)
                clrType = nullableType;

            using (CSharpCodeProvider compiler = new CSharpCodeProvider())
            {
                CodeTypeReference type = new CodeTypeReference(clrType);
                string result = compiler.GetTypeOutput(type);
                if (nullableType != null)
                    result = String.Concat(result, '?');

                return result;
            }
        }

        internal static Type ToClrType(this string cSharpTypeName)
        {
            CSharpTypeNames.TryGetValue(cSharpTypeName, out Type clrType);
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

        private static IEnumerable<KeyValuePair<string, Type>> LoadCSharpTypeNames()
        {
            Assembly mscorlib = Assembly.GetAssembly(typeof(int));
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                foreach (TypeInfo type in mscorlib.DefinedTypes)
                {
                    if (!String.Equals(type.Namespace, "System"))
                        continue;

                    string csTypeName = LoadCSharpTypeName(provider, type);
                    if (csTypeName == null)
                        continue;

                    yield return new KeyValuePair<string, Type>(csTypeName, type);

                    Type arrayType = type.MakeArrayType();
                    string csArrayTypeName = LoadCSharpTypeName(provider, arrayType);
                    yield return new KeyValuePair<string, Type>(csArrayTypeName, arrayType);
                }
            }
        }

        private static string LoadCSharpTypeName(CSharpCodeProvider provider, Type type)
        {
            CodeTypeReference typeRef = new CodeTypeReference(type);
            string csTypeName = provider.GetTypeOutput(typeRef);

            // Ignore qualified types.
            return csTypeName.IndexOf('.') == -1 ? csTypeName : null;
        }
    }
}