using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition
    {
        public ActionDefinitionTarget Target { get; }
        public ActionMethod Method { get; set; }
        public string Description { get; set; }
        public string ChildRoute { get; set; }
        public TypeReference BodyContract { get; set; }
        public string BodyBinder { get; set; }
        public bool IsAnonymous { get; set; }
        public IList<ActionParameter> Parameters { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.Parameters = new Collection<ActionParameter>();
        }
    }
}