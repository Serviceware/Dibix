using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ConsumerPackageManager
    {
        private readonly string _cpmFile;
        private readonly Lazy<Task<IReadOnlyDictionary<string, string>>> _packageVersionsAccessor;

        public ConsumerPackageManager(string cpmFile)
        {
            _cpmFile = cpmFile;
            _packageVersionsAccessor = new Lazy<Task<IReadOnlyDictionary<string, string>>>(CollectPackageVersionVariablesFromMSBuild);
        }

        public static ConsumerPackageManager Load(string consumerDirectory)
        {
            const string cpmFileName = "Directory.Packages.props";
            string cpmFile = Path.Combine(consumerDirectory, cpmFileName);
            if (!File.Exists(cpmFile))
                throw new CommandLineValidationException($"Consumer repository root '{consumerDirectory}' does define a '{cpmFileName}' file");

            return new ConsumerPackageManager(cpmFile);
        }

        private async Task<IReadOnlyDictionary<string, string>> CollectPackageVersionVariablesFromMSBuild() => await CollectPackageVersionVariablesFromMSBuildCore().ToDictionaryAsync().ConfigureAwait(false);
        private async IAsyncEnumerable<(string PackageName, string PackageVersion)> CollectPackageVersionVariablesFromMSBuildCore()
        {
            string standardOutput = await ProcessUtility.CaptureAsync(fileName: "dotnet", arguments: $"msbuild -getItem:PackageVersion \"{_cpmFile}\"").ConfigureAwait(false);
            JObject json = JObject.Parse(standardOutput);
            foreach (JObject item in json.SelectTokens("$.Items.PackageVersion[?(@.Identity =~ /^Dibix/)]").Cast<JObject>())
            {
                string packageName = GetRequiredJsonPropertyValue(item, "Identity");
                string packageVersion = GetRequiredJsonPropertyValue(item, "Version");
                yield return (packageName, packageVersion);
            }
        }

        private static string GetRequiredJsonPropertyValue(JObject container, string propertyName)
        {
            JProperty property = container.Property(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing property '{propertyName}' at '{container.Path}'");

            return (string)property.Value;
        }

        public async Task<string> GetPackageVersion(string packageName)
        {
            IReadOnlyDictionary<string, string> packageVersionMap = await _packageVersionsAccessor.Value.ConfigureAwait(false);
            if (!packageVersionMap.TryGetValue(packageName, out string packageVersion))
                throw new InvalidOperationException($"No package version registered in consumer for package '{packageName}'");

            return packageVersion;
        }
    }
}