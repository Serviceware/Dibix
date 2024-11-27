using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTargetDefinition
    {
        public ActionTarget Target { get; set; }
        public IList<ActionParameter> Parameters { get; } = new Collection<ActionParameter>();
        public IDictionary<string, PathParameter> PathParameters { get; } = new Dictionary<string, PathParameter>();
    }
}