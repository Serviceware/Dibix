using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinition : ActionTargetDefinition
    {
        public SourceLocation Location { get; set; }
        public ActionMethod Method { get; set; }
        public string OperationId { get; set; }
        public string Description { get; set; }
        public Token<string> ChildRoute { get; set; }
        public ActionRequestBody RequestBody { get; set; }
        public SecuritySchemeRequirements SecuritySchemes { get; } = new SecuritySchemeRequirements(SecuritySchemeOperator.Or);
        public ICollection<AuthorizationBehavior> Authorization { get; set; } = new Collection<AuthorizationBehavior>();
        public ActionCompatibilityLevel CompatibilityLevel { get; set; } = ActionCompatibilityLevel.Native;
    }
}