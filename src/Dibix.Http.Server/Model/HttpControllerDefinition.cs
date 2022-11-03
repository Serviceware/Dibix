using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Server
{
    public sealed class HttpControllerDefinition
    {
        public string AreaName { get; }
        public string ControllerName { get; }
        public IReadOnlyCollection<HttpActionDefinition> Actions { get; }
        public IReadOnlyCollection<string> ControllerImports { get; }

        internal HttpControllerDefinition(string areaName, string controllerName, IList<HttpActionDefinition> actions, IList<string> imports)
        {
            AreaName = areaName;
            ControllerName = controllerName;
            Actions = new ReadOnlyCollection<HttpActionDefinition>(actions);
            ControllerImports = new ReadOnlyCollection<string>(imports);
        }
    }
}