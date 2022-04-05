using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
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

        private sealed class AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy : ArtifactPackageHttpApiDiscoveryStrategy, IHttpApiDiscoveryStrategy
        {
            private readonly IOptionsMonitor<HostingOptions> _options;
            private readonly ILogger _logger;

            public AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy(IOptionsMonitor<HostingOptions> options, ILogger logger)
            {
                _options = options;
                _logger = logger;
            }

            protected override IEnumerable<Assembly> CollectAssemblies()
            {
                string currentDirectory = AppContext.BaseDirectory;
                string packagesDirectory = Path.Combine(currentDirectory, "Packages");

                foreach (string packageName in _options.CurrentValue.Packages)
                {
                    yield return CollectAssembly(packagesDirectory, packageName);
                }
            }

            private Assembly CollectAssembly(string packagesDirectory, string packageName)
            {
                const string kind = "Http extension";
                string filePath = Path.Combine(packagesDirectory, $"{packageName}.dbx");

                _logger.LogInformation("Loading package: {packagePath}", filePath);
                AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext($"Dibix {kind} '{packageName}'", filePath);
                byte[] content = Unwrap(filePath);
                using Stream stream = new MemoryStream(content);
                return assemblyLoadContext.LoadFromStream(stream);
            }
        }
    }
}