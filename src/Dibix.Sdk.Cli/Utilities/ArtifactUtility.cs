using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal static class ArtifactUtility
    {
        private static readonly ConcurrentDictionary<string, ConsumerPackageManager> ConsumerPackageManagers = new ConcurrentDictionary<string, ConsumerPackageManager>();

        public static string[] NuGetPackageNames { get; } =
        [
            "Dibix",
            "Dibix.Dapper",
            "Dibix.Http.Client",
            "Dibix.Http.Server",
            "Dibix.Http.Server.AspNet",
            "Dibix.Http.Server.AspNetCore",
            "Dibix.Sdk",
            "Dibix.Testing",
            "Dibix.Worker.Abstractions"
        ];
        public static string[] ApplicationNames { get; } =
        [
            "dibix-http-host",
            "dibix-worker-host"
        ];

        public static IReadOnlyDictionary<string, bool> AllArtifacts { get; } = ((KeyValuePair<string, bool>[])[
            ..NuGetPackageNames.Select(name => new KeyValuePair<string, bool>(name, false)),
            ..ApplicationNames.Select(name => new KeyValuePair<string, bool>(name, true))
        ]).ToDictionary(x => x.Key, x => x.Value);

        public static ConsumerPackageManager GetPackageManagerForConsumer(string consumerDirectory)
        {
            ConsumerPackageManager packageManager = ConsumerPackageManagers.GetOrAdd(consumerDirectory, ConsumerPackageManager.Load);
            return packageManager;
        }

        public static async Task<string> GetLocalDibixNuGetPackageVersion(CancellationToken cancellationToken)
        {
            string version = await CollectNBGVProperty("NuGetPackageVersion", cancellationToken).ConfigureAwait(false);
            return version;
        }

        public static async Task<string> GetLocalDibixDockerImageTag(CancellationToken cancellationToken)
        {
            Version version = new Version(await CollectNBGVProperty("Version", cancellationToken).ConfigureAwait(false));
            string tag = $"{version.Major}.{version.Minor}";
            return tag;
        }

        public static async Task CreateNuGetPackage(string packageName, string packageVersion, string configuration, CancellationToken cancellationToken)
        {
            string projectName = packageName == "Dibix.Sdk" ? "Dibix.Sdk.Cli" : packageName;
            string sourcePath = Path.Combine(KnownDirectory.DibixRootDirectory, "src", projectName, $"{projectName}.csproj");
            await ProcessUtility.Execute("dotnet", $"pack \"{sourcePath}\" --verbosity quiet --nologo --no-restore -p:PackageVersionOverride={packageVersion} -p:Configuration={configuration}", cancellationToken);
        }

        public static void RemovePackageFromNuGetPackageCache(string packageName, string packageVersion)
        {
            DirectoryInfo cacheDirectory = new DirectoryInfo(Path.Combine(KnownDirectory.PackageCacheDirectory, packageName, packageVersion));
            if (cacheDirectory.Exists)
                cacheDirectory.Delete(recursive: true);
        }

        public static void DeployPackageToNuGetPackageCache(string packageName, string packageVersion, string configuration)
        {
            string nupkgPath = EvaluateNuGetPackagePath(packageName, packageVersion, configuration);
            NuGetPackageExpander.Expand(packageName, packageVersion, nupkgPath, KnownDirectory.PackageCacheDirectory);
        }

        public static bool IsSdk(string packageName) => packageName == "Dibix.Sdk";

        public static async Task RestoreNuGetPackages(CancellationToken cancellationToken)
        {
            const string solutionName = "Dibix.slnx";
            string solutionPath = Path.Combine(KnownDirectory.DibixRootDirectory, solutionName);
            await ProcessUtility.Execute("dotnet", $"restore \"{solutionPath}\" --verbosity quiet --nologo", cancellationToken).ConfigureAwait(false);
        }

        public static async Task PushNuGetPackage(string packageName, string packageVersion, string configuration, string packageSource, string apiKey, CancellationToken cancellationToken)
        {
            string nupkgPath = EvaluateNuGetPackagePath(packageName, packageVersion, configuration);
            IDictionary<string, string> environmentVariables = new Dictionary<string, string> { ["NUGET_AUTH_TOKEN"] = apiKey };
            await ProcessUtility.Execute("dotnet", $"nuget push \"{nupkgPath}\" --source \"{packageSource}\" --skip-duplicate --api-key Dummy", cancellationToken, environmentVariables: environmentVariables).ConfigureAwait(false);
        }

        private static string EvaluateNuGetPackagePath(string packageName, string packageVersion, string configuration)
        {
            string projectName = packageName == "Dibix.Sdk" ? "Dibix.Sdk.Cli" : packageName;
            string nupkgPath = Path.Combine(KnownDirectory.DibixRootDirectory, "src", projectName, "bin", configuration, $"{packageName}.{packageVersion}.nupkg");
            return nupkgPath;
        }

        public static async Task UnlistNuGetPackage(string packageName, string packageVersion, string apiKey, CancellationToken cancellationToken)
        {
            try
            {
                _ = await ProcessUtility.Capture("dotnet", $"nuget delete \"{packageName}\" \"{packageVersion}\" --source https://nuget.org --api-key {apiKey} --non-interactive", cancellationToken).ConfigureAwait(false);
            }
            catch (ProcessExecutionException exception)
            {
                ConsoleUtility.WriteLineWarning(exception.Message.TrimEnd());
            }
        }

        public static async Task RemoveDockerImageFromDockerHub(string organization, string repository, string tag, string accessToken, CancellationToken cancellationToken)
        {
            using HttpClient client = CreateDockerHubClient(accessToken);
            HttpResponseMessage responseMessage = await client.DeleteAsync($"repositories/{organization}/{repository}/tags/{tag}", cancellationToken).ConfigureAwait(false);
            if (!responseMessage.IsSuccessStatusCode)
            {
#if NET
                string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                string responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                ConsoleUtility.WriteLineWarning($"""
                                                 Deleting docker image from Docker Hub failed: {(int)responseMessage.StatusCode} {responseMessage.StatusCode}
                                                 {responseContent}
                                                 """);
            }
        }

        public static async Task RemoveDockerImageFromLocalCache(string organization, string repository, string tag, CancellationToken cancellationToken)
        {
            await ProcessUtility.Execute("docker", $"rmi {organization}/{repository}:{tag} --force", cancellationToken).ConfigureAwait(false);
        }

        public static async Task<string> GetDockerHubAccessToken(string userName, string password)
        {
            using HttpClient client = CreateDockerHubClient();
            HttpResponseMessage responseMessage = await client.PostAsJsonAsync("users/login", new { username = userName, password = password }).ConfigureAwait(false);
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"""
                                                     Authenticating with Docker Hub failed: {(int)responseMessage.StatusCode} {responseMessage.StatusCode}
                                                     {await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)}
                                                     """);
            }

            JsonObject responseContent = await responseMessage.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false);
            const string propertyName = "token";
            JsonNode tokenNode = responseContent[propertyName];
            if (tokenNode == null)
            {
                throw new InvalidOperationException($"""
                                                     Missing expected property '{propertyName}' on Docker Hub authentication response
                                                     {responseContent.ToJsonString(new JsonSerializerOptions { WriteIndented = true })};
                                                     """);
            }

            string accessToken = tokenNode.GetValue<string>();
            return accessToken;
        }

        private static HttpClient CreateDockerHubClient(string accessToken = null)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri("https://hub.docker.com/v2/"),
                //DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(ThisAssembly.AssemblyName, ThisAssembly.AssemblyFileVersion) } }
            };

            if (accessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("JWT", accessToken);

            return client;
        }

        private static async Task<string> CollectNBGVProperty(string propertyName, CancellationToken cancellationToken)
        {
            string value = await ProcessUtility.Capture("nbgv", $"get-version --project \"{KnownDirectory.DibixRootDirectory}\" --variable {propertyName}", cancellationToken).ConfigureAwait(false);
            return value;
        }
    }
}