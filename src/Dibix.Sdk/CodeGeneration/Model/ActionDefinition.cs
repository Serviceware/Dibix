using System.Collections.Generic;

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
        public IDictionary<string, ActionParameterSource> ParameterSources { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.ParameterSources = new Dictionary<string, ActionParameterSource>();
        }
    }
}