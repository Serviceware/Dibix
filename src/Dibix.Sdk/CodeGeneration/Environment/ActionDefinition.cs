using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition
    {
        public ActionDefinitionTarget Target { get; }
        public ActionMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public string BodyContract { get; set; }
        public string BodyBinder { get; set; }
        public bool OmitResult { get; set; }
        public bool IsAnonymous { get; set; }
        public IDictionary<string, ActionParameterSource> DynamicParameters { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.DynamicParameters = new Dictionary<string, ActionParameterSource>();
        }
    }
}