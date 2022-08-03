using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix
{
    internal static class ReflectionExtensions
    {
        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            Guard.IsNotNull(assembly, nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
#pragma warning disable CS8619
                return e.Types.Where(t => t != null);
#pragma warning restore CS8619
            }
        }

#pragma warning disable CS8625
        public static MethodInfo SafeGetMethod(this Type type, string methodName, Type[] types) => SafeGetMethod(type, methodName, bindingFlags: DefaultLookup, types);
        public static MethodInfo SafeGetMethod(this Type type, string methodName) => SafeGetMethod(type, methodName, bindingFlags: DefaultLookup, null);
        public static MethodInfo SafeGetMethod(this Type type, string methodName, BindingFlags bindingFlags) => SafeGetMethod(type, methodName, bindingFlags, types: null);
        public static MethodInfo SafeGetMethod(this Type type, string methodName, BindingFlags bindingFlags, Type[] types)
#pragma warning restore CS8625
        {
#pragma warning disable CS8600
            MethodInfo method = types != null ? type.GetMethod(methodName, bindingFlags, binder: null, types, modifiers: null) : type.GetMethod(methodName, bindingFlags);
#pragma warning restore CS8600
            if (method == null)
                throw new InvalidOperationException($"Could not find method {type}.{methodName}({String.Join(", ", (types ?? Enumerable.Empty<Type>()).Select(x => x.FullName))}) [{bindingFlags}]");

            return method;
        }
    }
}