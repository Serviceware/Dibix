using System.Collections.Generic;
using System.Linq;

namespace Dibix.Http.Server
{
    public sealed class HttpControllerDefinition
    {
        public string ControllerName { get; }
        public IReadOnlyCollection<HttpActionDefinition> Actions { get; }

        internal HttpControllerDefinition(string controllerName, IEnumerable<HttpActionDefinition> actions)
        {
            ControllerName = controllerName;
            Actions = actions.ToArray();

            foreach (HttpActionDefinition action in Actions)
            {
                action.Controller = this;
            }
        }
    }
}