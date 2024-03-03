using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Dibix.Hosting.Abstractions;
using Dibix.Http.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class HttpApiRegistryFactory
    {
        private readonly IOptionsMonitor<HostingOptions> _options;
        private readonly ILogger<HttpApiRegistryFactory> _logger;

        public HttpApiRegistryFactory(IOptionsMonitor<HostingOptions> options, ILogger<HttpApiRegistryFactory> logger)
        {
            _options = options;
            _logger = logger;
        }

        public IHttpApiRegistry Create()
        {
            IHttpApiDiscoveryStrategy strategy = new AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy(_options, _logger);
            return HttpApiRegistry.Discover(strategy);
        }

        private sealed class AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy : AssemblyHttpApiDiscoveryStrategy, IHttpApiDiscoveryStrategy
        {
            private readonly IOptionsMonitor<HostingOptions> _options;
            private readonly ILogger _logger;

            public AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy(IOptionsMonitor<HostingOptions> options, ILogger logger)
            {
                _options = options;
                _logger = logger;
            }

            protected override IEnumerable<HttpApiDescriptor> CollectApiDescriptors()
            {
                string currentDirectory = AppContext.BaseDirectory;
                string packagesDirectory = Path.Combine(currentDirectory, "Packages");

                foreach (string packageName in _options.CurrentValue.Packages)
                {
                    const string kind = "Http extension";
                    string packagePath = Path.Combine(packagesDirectory, $"{packageName}.dbx");

                    _logger.LogInformation("Loading package: {packagePath}", packagePath);
                    AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext($"Dibix {kind} '{packageName}'", packagePath);

                    using Package package = Package.Open(packagePath, FileMode.Open, FileAccess.Read);
                    
                    Assembly assembly = LoadAssembly(package, assemblyLoadContext);
                    ArtifactPackageMetadata metadata = ReadPackageMetadata(package);

                    HttpApiDescriptor apiDescriptor = CollectApiDescriptor(assembly);
                    apiDescriptor.Metadata.ProductName = package.PackageProperties.Title;
                    apiDescriptor.Metadata.AreaName = package.PackageProperties.Subject;
                    yield return apiDescriptor;
                }
            }

            private static Assembly LoadAssembly(Package package, AssemblyLoadContext assemblyLoadContext)
            {
                Stream inputStream = GetPartStream(package, "Content");
                using MemoryStream outputStream = new MemoryStream();
                inputStream.CopyTo(outputStream);
                byte[] data = outputStream.ToArray();

                using Stream stream = new MemoryStream(data);
                Assembly assembly = assemblyLoadContext.LoadFromStream(stream);
                return assembly;
            }

            private static ArtifactPackageMetadata ReadPackageMetadata(Package package)
            {
                using Stream stream = GetPartStream(package, "Metadata");
                JsonSerializerOptions options = JsonSerializerOptions.Default;
                ArtifactPackageMetadata metadata = JsonSerializer.Deserialize<ArtifactPackageMetadata>(stream, options)!;
                return metadata;
            }

            private static Stream GetPartStream(Package package, string name)
            {
                Uri contentUri = new Uri(name, UriKind.Relative);
                Uri partUri = PackUriHelper.CreatePartUri(contentUri);
                PackagePart part = package.GetPart(partUri);
                Stream stream = part.GetStream();
                return stream;
            }
        }
    }
}