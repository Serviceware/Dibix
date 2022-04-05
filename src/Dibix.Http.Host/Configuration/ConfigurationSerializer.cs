using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Dibix.Http.Host
{
    internal static class ConfigurationSerializer
    {
        public static IResult DumpConfiguration(IConfigurationRoot root)
        {
            JsonObject jsonObject = new JsonObject();
            DumpConfiguration(jsonObject, parentNode: null, parentKey: null, root.GetChildren(), root);
            string json = jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            IResult result = Results.Text(json, contentType: "application/json");
            return result;
        }
        private static void DumpConfiguration(JsonNode node, JsonNode? parentNode, string? parentKey, IEnumerable<IConfigurationSection> children, IConfigurationRoot root)
        {
            foreach (IConfigurationSection child in children)
            {
                if (child.Key == "")
                {
                    // :ASPNETCORE_BROWSER_TOOLS
                    // This key is treated as a section because it begins with :
                    continue;
                }

                ConfigurationValueResolutionResult result = ResolveConfigurationValue(root, child.Path, out string? value);
                JsonNode childNode = DumpConfiguration(result, child.Key, parentKey, value, node, parentNode);
                DumpConfiguration(childNode, node, child.Key, child.GetChildren(), root);
            }
        }
        private static JsonNode DumpConfiguration(ConfigurationValueResolutionResult result, string key, string? parentKey, string? value, JsonNode node, JsonNode? parentNode)
        {
            switch (result)
            {
                case ConfigurationValueResolutionResult.NotInterested:
                    break;

                case ConfigurationValueResolutionResult.NotFound:
                    {
                        JsonObject childNode = new JsonObject();
                        node[key] = childNode;
                        return childNode;
                    }

                case ConfigurationValueResolutionResult.Found:
                    {
                        if (Int32.TryParse(key, out int intValue))
                        {
                            if (parentNode![parentKey!] is not JsonArray array)
                            {
                                array = new JsonArray();
                                parentNode![parentKey!] = array;
                            }
                            array.Insert(intValue, value!);
                        }
                        else
                        {
                            node[key] = value!;
                        }

                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            return node;
        }

        private static ConfigurationValueResolutionResult ResolveConfigurationValue(IConfigurationRoot root, string key, out string? value)
        {
            foreach (IConfigurationProvider configurationProvider in root.Providers.Reverse())
            {
                if (!configurationProvider.TryGet(key, out value))
                    continue;

                bool isAppSettingsJsonProvider = configurationProvider is JsonConfigurationProvider jsonConfigurationProvider && Regex.IsMatch(jsonConfigurationProvider.Source.Path ?? "", @"^appsettings(.+)?|secrets\.json$");
                return isAppSettingsJsonProvider ? ConfigurationValueResolutionResult.Found : ConfigurationValueResolutionResult.NotInterested;
            }

            value = null;
            return ConfigurationValueResolutionResult.NotFound;
        }

        private enum ConfigurationValueResolutionResult
        {
            None,
            NotInterested,
            NotFound,
            Found

        }
    }
}