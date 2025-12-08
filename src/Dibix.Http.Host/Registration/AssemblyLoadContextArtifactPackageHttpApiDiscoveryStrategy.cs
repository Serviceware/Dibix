using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dibix.Hosting.Abstractions;
using Dibix.Http.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy : AssemblyHttpApiDiscoveryStrategy, IHttpApiDiscoveryStrategy
    {
        private readonly IOptionsMonitor<HostingOptions> _options;
        private readonly ILogger _logger;

        public AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy(IOptionsMonitor<HostingOptions> options, ILogger<AssemblyLoadContextArtifactPackageHttpApiDiscoveryStrategy> logger)
        {
            _options = options;
            _logger = logger;
        }

        protected override IEnumerable<HttpApiDescriptor> CollectApiDescriptors()
        {
            string packagesDirectory = ApplicationEnvironment.PackagesDirectory;

            foreach (string packageName in _options.CurrentValue.Packages)
            {
                const string kind = "Http extension";
                string packagePath = Path.Combine(packagesDirectory, $"{packageName}.{ApplicationEnvironment.PackageExtension}");

                _logger.LogInformation("Loading package: {packagePath}", packagePath);
                AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext($"Dibix {kind} '{packageName}'", packagePath);

                using Package package = Package.Open(packagePath, FileMode.Open, FileAccess.Read);

                Assembly assembly = LoadAssembly(package, assemblyLoadContext);
                ArtifactPackageMetadata metadata = ReadPackageMetadata(package);
                var metadataMap = metadata.Controllers
                                          .SelectMany(x => x.Actions, (x, y) => (x.ControllerName, Action: y))
                                          .ToDictionary(x => (x.ControllerName, x.Action.ActionName), x => x.Action);

                HttpApiDescriptor apiDescriptor = CollectApiDescriptor(assembly);
                apiDescriptor.Metadata.ProductName = package.PackageProperties.Title;
                apiDescriptor.Metadata.AreaName = package.PackageProperties.Subject;
                apiDescriptor.ActionConfiguredHandler = (controllerName, actionBuilder) => OnActionConfigured(controllerName, actionBuilder, metadataMap);

                yield return apiDescriptor;
            }
        }

        private static void OnActionConfigured(string controllerName, IHttpActionDefinitionBuilder actionBuilder, IReadOnlyDictionary<(string ControllerName, string ActionName), HttpActionDefinitionMetadata> metadata)
        {
            HttpActionDefinitionMetadata actionMetadata = metadata[(controllerName, actionBuilder.ActionName)];
            //actionBuilder.ActionName = actionMetadata.ActionName;
            actionBuilder.RelativeNamespace = actionMetadata.RelativeNamespace;
            actionBuilder.Method = actionMetadata.Method;
            actionBuilder.ChildRoute = actionMetadata.ChildRoute;
            actionBuilder.FileResponse = actionMetadata.FileResponse;
            actionBuilder.Description = actionMetadata.Description;
            actionBuilder.ModelContextProtocolType = actionMetadata.ModelContextProtocolType;
            actionBuilder.SecuritySchemes.AddRange(actionMetadata.SecuritySchemes);

            foreach ((int statusCode, HttpErrorResponse errorResponse) in actionMetadata.StatusCodeDetectionResponses)
            {
                int errorCode = errorResponse.ErrorCode;
                string errorMessage = errorResponse.ErrorMessage;
                actionBuilder.SetStatusCodeDetectionResponse(statusCode, errorCode, errorMessage);
            }

            if (actionBuilder is IHttpActionDefinitionBuilderInternal internalActionBuilder)
            {
                foreach (string requiredClaim in actionMetadata.RequiredClaims)
                {
                    internalActionBuilder.RegisterRequiredClaim(requiredClaim);
                }

                foreach (var (parameterName, parameterDescription) in actionMetadata.ParameterDescriptions)
                {
                    internalActionBuilder.AddParameterDescription(parameterName, parameterDescription);
                }
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
            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General) { Converters = { new JsonStringEnumConverter() } };
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