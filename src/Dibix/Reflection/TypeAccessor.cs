using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix
{
    public static class TypeAccessor
    {
        private static readonly ConcurrentDictionary<Type, IDictionary<string, PropertyAccessor>> Cache = new ConcurrentDictionary<Type, IDictionary<string, PropertyAccessor>>();

        public static IEnumerable<PropertyAccessor> GetProperties<T>() => GetProperties(typeof(T));
        public static IEnumerable<PropertyAccessor> GetProperties(Type type) => Cache.GetOrAdd(type, ReadProperties).Values;

        public static PropertyAccessor GetProperty<T>(string propertyName) => GetProperty(typeof(T), propertyName);
        public static PropertyAccessor GetProperty(Type type, string propertyName) => Cache.GetOrAdd(type, ReadProperties)[propertyName.ToUpperInvariant()];

        private static IDictionary<string, PropertyAccessor> ReadProperties(Type type)
        {
            return type.GetRuntimeProperties()
                       .Select(PropertyAccessor.Create)
                       .ToDictionary(x => x.Name.ToUpperInvariant());
        }
    }
}
