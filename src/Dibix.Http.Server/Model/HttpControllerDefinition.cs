using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Server
{
    public sealed class HttpControllerDefinition
    {
        private readonly EndpointMetadata _endpointMetadata;
        private string _fullName;

        public string ControllerName { get; }
        public string FullName => _fullName ??= $"{_endpointMetadata.ProductName}.{_endpointMetadata.AreaName}";
        public IReadOnlyCollection<HttpActionDefinition> Actions { get; }

        internal HttpControllerDefinition(EndpointMetadata endpointMetadata, string controllerName, IList<HttpActionDefinition> actions)
        {
            _endpointMetadata = endpointMetadata;
            ControllerName = controllerName;
            Actions = new ReadOnlyCollection<HttpActionDefinition>(actions);
        }
    }
}