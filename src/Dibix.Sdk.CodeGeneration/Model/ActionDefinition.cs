using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition : ActionTargetDefinition
    {
        public ActionMethod Method { get; set; }
        public string OperationId { get; set; }
        public string Description { get; set; }
        public Token<string> ChildRoute { get; set; }
        public ActionRequestBody RequestBody { get; set; }
        public ICollection<ICollection<string>> SecuritySchemes { get; }
        public AuthorizationBehavior Authorization { get; set; }

        public ActionDefinition()
        {
            SecuritySchemes = new Collection<ICollection<string>>();
        }
    }
}