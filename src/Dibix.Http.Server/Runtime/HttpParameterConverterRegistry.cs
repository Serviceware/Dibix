using System;
using System.Collections.Generic;

namespace Dibix.Http.Server
{
    public static class HttpParameterConverterRegistry
    {
        private static readonly IDictionary<string, Lazy<IHttpParameterConverter>> Map = new Dictionary<string, Lazy<IHttpParameterConverter>>();

        public static void Register<T>(string name) where T : IHttpParameterConverter, new() => Map.Add(name, new Lazy<IHttpParameterConverter>(() => new T()));

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