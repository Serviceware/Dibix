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
            ICollection<string> knownPropertyPaths = root.Providers
                                                         .Where(IsLocalConfigurationProvider)
                                                         .SelectMany(Flatten)
                                                         .Distinct()
                                                         .OrderBy(x => x)
                                                         .ToArray();

            DumpConfiguration(jsonObject, parentNode: null, parentKey: null, root.GetChildren(), root, knownPropertyPaths);
            string json = jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            IResult result = Results.Text(json, contentType: "application/json");
            return result;
        }
        private static void DumpConfiguration(JsonNode node, JsonNode? parentNode, string? parentKey, IEnumerable<IConfigurationSection> children, IConfigurationRoot root, ICollection<string> knownPropertyPaths)
        {
            foreach (IConfigurationSection child in children)
            {
                if (child.Key == "")
                {
                    // :ASPNETCORE_BROWSER_TOOLS
                    // This key is treated as a section because it begins with :
                    continue;
                }

                string normalizedPath = NormalizePath(child.Path);
                bool isKnownPropertyPath = knownPropertyPaths.Any(x => x.StartsWith(normalizedPath, StringComparison.Ordinal));
                if (!isKnownPropertyPath)
                    continue;
                
                bool isSection = !TryResolveConfigurationValue(root, child.Path, out string? value);
                JsonNode childNode = DumpConfiguration(isSection, child.Key, parentKey, value, node, parentNode);
                DumpConfiguration(childNode, node, child.Key, child.GetChildren(), root, knownPropertyPaths);
            }
        }

        private static JsonNode DumpConfiguration(bool isSection, string key, string? parentKey, string? value, JsonNode node, JsonNode? parentNode)
        {
            if (isSection)
            {
                JsonObject childNode = new JsonObject();
                node[key] = childNode;
                return childNode;
            }

            if (Int32.TryParse(key, out int intValue))
            {
                if (parentNode![parentKey!] is not JsonArray array)
                {
                    array = new JsonArray();
                    parentNode[parentKey!] = array;
                }

                array.Insert(intValue, value!);
            }
            else
            {
                node[key] = value!;
            }

            return node;
        }

        private static IEnumerable<string> Flatten(IConfigurationProvider configurationProvider) => Flatten(configurationProvider, GetChildren(configurationProvider, parentPath: null), parentPath: null);
        private static IEnumerable<string> Flatten(IConfigurationProvider configurationProvider, IEnumerable<string> childKeys, string? parentPath)
        {
            foreach (string childKey in childKeys)
            {
                string currentPath = parentPath != null ? $"{parentPath}:{childKey}" : childKey;
                ICollection<string> subKeys = GetChildren(configurationProvider, parentPath: currentPath);
                if (subKeys.Any())
                {
                    foreach (string path in Flatten(configurationProvider, subKeys, parentPath: currentPath))
                    {
                        yield return path;
                    }
                }
                else
                {
                    yield return currentPath;
                }
            }
        }

        private static ICollection<string> GetChildren(IConfigurationProvider configurationProvider, string? parentPath) => configurationProvider.GetChildKeys(Enumerable.Empty<string>(), parentPath).Distinct().ToArray();

        private static bool IsLocalConfigurationProvider(IConfigurationProvider configurationProvider)
        {
            bool isLocalConfigurationProvider = configurationProvider is JsonConfigurationProvider jsonConfigurationProvider && Regex.IsMatch(jsonConfigurationProvider.Source.Path ?? "", @"^appsettings(.+)?|secrets\.json$");
            return isLocalConfigurationProvider;
        }

        private static string NormalizePath(string childPath)
        {
            const char delimiter = ':';
            IList<string> tokens = childPath.Split(delimiter).ToList();
            if (!Int32.TryParse(tokens.Last(), out _)) 
                return childPath;

            tokens.RemoveAt(tokens.Count - 1);
            return String.Join(delimiter, tokens);
        }

        private static bool TryResolveConfigurationValue(IConfigurationRoot root, string key, out string? value)
        {
            foreach (IConfigurationProvider configurationProvider in root.Providers.Reverse())
            {
                if (!configurationProvider.TryGet(key, out value))
                    continue;

                return true;
            }

            value = null;
            return false;
        }
    }
}