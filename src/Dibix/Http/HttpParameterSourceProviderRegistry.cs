using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dibix.Http
{
    public static class HttpParameterSourceProviderRegistry
    {
        private static readonly IDictionary<string, Lazy<IHttpParameterSourceProvider>> Map = new Dictionary<string, Lazy<IHttpParameterSourceProvider>>();

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static HttpParameterSourceProviderRegistry()
        {
            Register<BodyParameterSourceProvider>(BodyParameterSourceProvider.SourceName);
            Register<RequestParameterSourceProvider>(RequestParameterSourceProvider.SourceName);
            Register<EnvironmentParameterSourceProvider>(EnvironmentParameterSourceProvider.SourceName);
        }

        public static void Register<T>(string name) where T : IHttpParameterSourceProvider, new() => Map.Add(name, new Lazy<IHttpParameterSourceProvider>(() => new T()));

        public static bool TryGetProvider(string name, out IHttpParameterSourceProvider provider)
        {
            if (Map.TryGetValue(name, out Lazy<IHttpParameterSourceProvider> providerAccessor))
            {
                provider = providerAccessor.Value;
                return true;
            }

            provider = null;
            return false;
        }
    }
}