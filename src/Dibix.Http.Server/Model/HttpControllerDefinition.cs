using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Server
{
    public sealed class HttpControllerDefinition
    {
        public string ControllerName { get; }
        public IReadOnlyCollection<HttpActionDefinition> Actions { get; }

        internal HttpControllerDefinition(string controllerName, IList<HttpActionDefinition> actions)
        {
            ControllerName = controllerName;
            Actions = new ReadOnlyCollection<HttpActionDefinition>(actions);
        }
    }
}