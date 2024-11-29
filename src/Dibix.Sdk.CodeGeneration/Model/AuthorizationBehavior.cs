using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class AuthorizationBehavior : ActionTargetDefinition
    {
        public ActionDefinition Parent { get; }

        public AuthorizationBehavior(ActionDefinition parent, ActionTarget actionTarget) : base(actionTarget)
        {
            Parent = parent;
        }

        public override void RegisterErrorResponse(int statusCode, int errorCode, string errorDescription, SourceLocation sourceLocation, ILogger logger)
        {
            base.RegisterErrorResponse(statusCode, errorCode, errorDescription, sourceLocation, logger);
            Parent.RegisterErrorResponse(statusCode, errorCode, errorDescription, sourceLocation, logger);
        }
    }
}