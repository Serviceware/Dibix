using System.Collections.Generic;
using System.Linq;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    internal sealed class AssemblyEndpointMetadataProvider : IEndpointMetadataProvider
    {
        private readonly IHttpApiRegistry _apiRegistry;
        private readonly IEndpointUrlBuilder _endpointUrlBuilder;

        public AssemblyEndpointMetadataProvider(IHttpApiRegistry apiRegistry, IEndpointUrlBuilder endpointUrlBuilder)
        {
            _apiRegistry = apiRegistry;
            _endpointUrlBuilder = endpointUrlBuilder;
        }

        public IEnumerable<EndpointDefinition> GetEndpoints()
        {
            ICollection<EndpointDefinition> endpoints = GetEndpointsCore().ToArray();
            return endpoints;
        }

        private IEnumerable<EndpointDefinition> GetEndpointsCore()
        {
            return from area in _apiRegistry.GetApis()
                   from controller in area.Controllers
                   from action in controller.Actions
                   let method = action.Method.ToString().ToUpperInvariant()
                   let url = _endpointUrlBuilder.BuildUrl(controller.AreaName, controller.ControllerName, action.ChildRoute)
                   select new EndpointDefinition(method, url, action);
        }
    }
}