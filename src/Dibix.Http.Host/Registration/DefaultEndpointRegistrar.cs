using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class DefaultEndpointRegistrar : IEndpointRegistrar
    {
        private readonly IEndpointMetadataProvider _endpointMetadataProvider;
        private readonly IEndpointImplementationProvider _endpointImplementationProvider;
        private readonly IOptions<HostingOptions> _hostingOptions;
        private readonly ILogger<DefaultEndpointRegistrar> _logger;

        public DefaultEndpointRegistrar(IEndpointMetadataProvider endpointMetadataProvider, IEndpointImplementationProvider endpointImplementationProvider, IOptions<HostingOptions> hostingOptions, ILogger<DefaultEndpointRegistrar> logger)
        {
            _endpointMetadataProvider = endpointMetadataProvider;
            _endpointImplementationProvider = endpointImplementationProvider;
            _hostingOptions = hostingOptions;
            _logger = logger;
        }

        public void Register(IEndpointRouteBuilder builder)
        {
            foreach (EndpointDefinition endpoint in _endpointMetadataProvider.GetEndpoints())
            {
                string baseAddress = "";
                if (!String.IsNullOrEmpty(_hostingOptions.Value.BaseAddress))
                    baseAddress = $"/{_hostingOptions.Value.BaseAddress.Trim('/')}";

                string route = $"{baseAddress}{endpoint.Url}";
                _logger.LogDebug("Registering route: {method} {route}", endpoint.Method, route);

                IEndpointConventionBuilder endpointBuilder = builder.MapMethods(route, EnumerableExtensions.Create(endpoint.Method), _endpointImplementationProvider.GetImplementation(endpoint));

                _ = endpointBuilder.WithMetadata(endpoint);

                foreach (string securityScheme in endpoint.ActionDefinition.SecuritySchemes) 
                    _ = securityScheme == SecuritySchemeNames.Anonymous ? endpointBuilder.AllowAnonymous() : endpointBuilder.RequireAuthorization(securityScheme);
            }
        }
    }
}