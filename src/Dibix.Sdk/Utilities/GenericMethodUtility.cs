using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk
{
    internal static class GenericMethodUtility
    {
        public static MethodInfo EnumerableSelectMethod { get; } = GetEnumerableSelectMethod();

        private static MethodInfo GetEnumerableSelectMethod()
        {
            foreach (MethodInfo method in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Select")
                    continue;

                Type[] genericArguments = method.GetGenericArguments();
                if (genericArguments.Length != 2)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;

                // IEnumerable<TSource> source
                if (parameters[0].ParameterType != typeof(IEnumerable<>).MakeGenericType(genericArguments[0]))
                    continue;

                // Func<TSource, TResult> selector
                if (parameters[1].ParameterType != typeof(Func<,>).MakeGenericType(genericArguments))
                    continue;

                return method;
            }
            throw new InvalidOperationException("Could not find method Enumerable.Select<TSource, TReturn>(IEnumerable<TSource> source, Func<TSource, TReturn> selector)");
        }
    }
}