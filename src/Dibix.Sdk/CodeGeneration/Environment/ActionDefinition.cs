using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition
    {
        public ActionDefinitionTarget Target { get; }
        public ActionMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public bool OmitResult { get; set; }
        public ICollection<ActionParameterMapping> DynamicParameters { get; }

        public ActionDefinition(ActionDefinitionTarget target)
        {
            this.Target = target;
            this.DynamicParameters = new Collection<ActionParameterMapping>();
        }
    }
}