using System;
using System.Collections.Concurrent;

namespace Dibix.Http.Server
{
    public static class HttpParameterConverterRegistry
    {
        private static readonly ConcurrentDictionary<string, Lazy<IHttpParameterConverter>> Map = new ConcurrentDictionary<string, Lazy<IHttpParameterConverter>>();

        public static void Register<T>(string name) where T : IHttpParameterConverter, new() => Map.TryAdd(name, new Lazy<IHttpParameterConverter>(() => new T()));

        public static bool TryGetConverter(string name, out IHttpParameterConverter converter)
        {
            if (Map.TryGetValue(name, out Lazy<IHttpParameterConverter> converterAccessor))
            {
                converter = converterAccessor.Value;
                return true;
            }

            converter = null;
            return false;
        }
    }
}