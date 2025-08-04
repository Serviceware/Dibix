using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal sealed class HttpEndpointRegistrar : IEndpointRegistrar
    {
        private readonly IEndpointMetadataProvider _endpointMetadataProvider;
        private readonly IEndpointImplementationProvider _endpointImplementationProvider;
        private readonly ILogger<HttpEndpointRegistrar> _logger;

        public HttpEndpointRegistrar(IEndpointMetadataProvider endpointMetadataProvider, IEndpointImplementationProvider endpointImplementationProvider, ILogger<HttpEndpointRegistrar> logger)
        {
            _endpointMetadataProvider = endpointMetadataProvider;
            _endpointImplementationProvider = endpointImplementationProvider;
            _logger = logger;
        }

        public void Register(IEndpointRouteBuilder builder)
        {
            foreach (EndpointDefinition endpoint in _endpointMetadataProvider.GetEndpoints())
            {
                string route = $"{endpoint.Url}";
                _logger.LogDebug("Registering route: {method} {route}", endpoint.Method, route);

                IEndpointConventionBuilder endpointBuilder = builder.MapMethods(route, EnumerableExtensions.Create(endpoint.Method), _endpointImplementationProvider.GetImplementation(endpoint));

                _ = endpointBuilder.WithMetadata(endpoint);

                foreach (string securityScheme in endpoint.ActionDefinition.SecuritySchemes)
                    _ = securityScheme == SecuritySchemeNames.Anonymous ? endpointBuilder.AllowAnonymous() : endpointBuilder.RequireAuthorization(securityScheme);
            }
        }
    }
}