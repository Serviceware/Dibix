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
        private readonly IOptions<CustomAuthenticationOptions> _customAuthenticationOptions;
        private readonly ILogger<DefaultEndpointRegistrar> _logger;

        public DefaultEndpointRegistrar(IEndpointMetadataProvider endpointMetadataProvider, IEndpointImplementationProvider endpointImplementationProvider, IOptions<HostingOptions> hostingOptions, IOptions<CustomAuthenticationOptions> customAuthenticationOptions, ILogger<DefaultEndpointRegistrar> logger)
        {
            _endpointMetadataProvider = endpointMetadataProvider;
            _endpointImplementationProvider = endpointImplementationProvider;
            _hostingOptions = hostingOptions;
            _customAuthenticationOptions = customAuthenticationOptions;
            _logger = logger;
        }

        public void Register(IEndpointRouteBuilder builder)
        {
            foreach (EndpointDefinition endpoint in _endpointMetadataProvider.GetEndpoints())
            {
                string baseAddress = "";
                if (_hostingOptions.Value.BaseAddress != null)
                    baseAddress = $"/{_hostingOptions.Value.BaseAddress.Trim('/')}";

                string route = $"{baseAddress}{endpoint.Url}";
                _logger.LogDebug("Registering route: {route}", route);

                IEndpointConventionBuilder endpointBuilder = builder.MapMethods(route, EnumerableExtensions.Create(endpoint.Method), _endpointImplementationProvider.GetImplementation(endpoint));

                endpointBuilder.WithMetadata(endpoint);
                
                if (!endpoint.ActionDefinition.IsAnonymous)
                {
                    CustomAuthenticationOptions customAuthenticationOptions = _customAuthenticationOptions.Value;
                    string policyName = customAuthenticationOptions.EndpointFilter(endpoint.ActionDefinition) ? CustomAuthenticationOptions.SchemeName : AuthenticationOptions.SchemeName;
                    endpointBuilder.RequireAuthorization(policyName);
                }
            }
        }
    }
}