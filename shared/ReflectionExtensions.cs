using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
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
    }
}