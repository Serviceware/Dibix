using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Dibix.Http.Host
{
    internal static class ConfigurationManagerCache
    {
        private static readonly ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>?> Cache = new ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>?>();

        public static IConfigurationManager<OpenIdConnectConfiguration>? Get(string name) => Cache.TryGetValue(name, out IConfigurationManager<OpenIdConnectConfiguration>? value) ? value : null;

        public static bool TryAdd(string name, IConfigurationManager<OpenIdConnectConfiguration>? configurationManager) => Cache.TryAdd(name, configurationManager);
    }
}