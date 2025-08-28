using System.Collections.Generic;

namespace Dibix
{
    internal sealed class HttpControllerDefinitionMetadata
    {
        public string ControllerName { get; }
        public IReadOnlyCollection<HttpActionDefinitionMetadata> Actions { get; }

        public HttpControllerDefinitionMetadata(string controllerName, IReadOnlyCollection<HttpActionDefinitionMetadata> actions)
        {
            ControllerName = controllerName;
            Actions = actions ?? [];
        }
    }
}